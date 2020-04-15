#!/bin/sh

# This script starts an instance of Xvfb, the "fake" X server, runs a command
# with that server available, and kills the X server when done.  The return
# value of the command becomes the return value of this script, except in cases
# where this script encounters an error.
#
# If anyone is using this to build a Debian package, make sure the package
# Build-Depends on xvfb and xauth.

set -e

# allow settings to be updated via environment
: "${xvfb_lockdir:=$HOME/.xvfb-locks}"
: "${xvfb_display_min:=99}"
: "${xvfb_display_max:=599}"

# assuming only one user will use this, let's put the locks in our own home directory
# avoids vulnerability to symlink attacks.
mkdir -p -- "$xvfb_lockdir" || exit

PROGNAME=xvfb-run
SERVERNUM=99
AUTHFILE=
ERRORFILE=/dev/null
XVFBARGS="-screen 0 1280x960x16"
LISTENTCP="-nolisten tcp"
XAUTHPROTO=.

# Query the terminal to establish a default number of columns to use for
# displaying messages to the user.  This is used only as a fallback in the event
# the COLUMNS variable is not set.  ($COLUMNS can react to SIGWINCH while the
# script is running, and this cannot, only being calculated once.)
DEFCOLUMNS=$(stty size 2>/dev/null | awk '{print $2}') || true
if ! expr "$DEFCOLUMNS" : "[[:digit:]]\+$" >/dev/null 2>&1; then
    DEFCOLUMNS=80
fi

# Display a message, wrapping lines at the terminal width.
message () {
    echo "$PROGNAME: $*" | fmt -t -w ${COLUMNS:-$DEFCOLUMNS}
}

# Display an error message.
error () {
    message "error: $*" >&2
}

# Display a usage message.
usage () {
    if [ -n "$*" ]; then
        message "usage error: $*"
    fi
    cat <<EOF
Usage: $PROGNAME [OPTION ...] COMMAND
Run COMMAND (usually an X client) in a virtual X server environment.
Options:
-a        --auto-servernum          try to get a free server number, starting at
                                    --server-num
-e FILE   --error-file=FILE         file used to store xauth errors and Xvfb
                                    output (default: $ERRORFILE)
-f FILE   --auth-file=FILE          file used to store auth cookie
                                    (default: ./.Xauthority)
-h        --help                    display this usage message and exit
-n NUM    --server-num=NUM          server number to use (default: $SERVERNUM)
-l        --listen-tcp              enable TCP port listening in the X server
-p PROTO  --xauth-protocol=PROTO    X authority protocol name to use
                                    (default: xauth command's default)
-s ARGS   --server-args=ARGS        arguments (other than server number and
                                    "-nolisten tcp") to pass to the Xvfb server
                                    (default: "$XVFBARGS")
EOF
}

# Find a free server number by looking at .X*-lock files in /tmp.
find_free_servernum() {
    i=$xvfb_display_min     # minimum display number
    while [ $i -lt $xvfb_display_max ]; do
        if [ -f "/tmp/.X$i-lock" ]; then                # still avoid an obvious open display
            i=$(($i + 1))
            continue
        fi
        exec 5>"$xvfb_lockdir/$i" || continue           # open a lockfile
        if flock -x -n 5; then                          # try to lock it
            SERVERNUM=$i
            break
        fi
        i=$(($i + 1))
    done
}

# Clean up files
clean_up() {
    if [ -n "$SERVERNUM" ]; then
        rm "$xvfb_lockdir/$SERVERNUM"
    fi

    if [ -e "$AUTHFILE" ]; then
        XAUTHORITY=$AUTHFILE xauth remove ":$SERVERNUM" >>"$ERRORFILE" 2>&1
    fi
    if [ -n "$XVFB_RUN_TMPDIR" ]; then
        if ! rm -r "$XVFB_RUN_TMPDIR"; then
            error "problem while cleaning up temporary directory"
            exit 5
        fi
    fi
    if [ -n "$XVFBPID" ]; then
        kill "$XVFBPID" >>"$ERRORFILE" 2>&1
    fi
}

# tidy up after ourselves
trap clean_up EXIT

# Parse the command line.
ARGS=$(getopt --options +ae:f:hn:lp:s:w: \
       --long auto-servernum,error-file:,auth-file:,help,server-num:,listen-tcp,xauth-protocol:,server-args:,wait: \
       --name "$PROGNAME" -- "$@")
GETOPT_STATUS=$?

if [ $GETOPT_STATUS -ne 0 ]; then
    error "internal error; getopt exited with status $GETOPT_STATUS"
    exit 6
fi

eval set -- "$ARGS"

while :; do
    case "$1" in
        -a|--auto-servernum) find_free_servernum; AUTONUM="yes" ;;
        -e|--error-file) ERRORFILE="$2"; shift ;;
        -f|--auth-file) AUTHFILE="$2"; shift ;;
        -h|--help) SHOWHELP="yes" ;;
        -n|--server-num) SERVERNUM="$2"; shift ;;
        -l|--listen-tcp) LISTENTCP="" ;;
        -p|--xauth-protocol) XAUTHPROTO="$2"; shift ;;
        -s|--server-args) XVFBARGS="$2"; shift ;;
        -w|--wait) shift ;;
        --) shift; break ;;
        *) error "internal error; getopt permitted \"$1\" unexpectedly"
           exit 6
           ;;
    esac
    shift
done

if [ "$SHOWHELP" ]; then
    usage
    exit 0
fi

if [ -z "$*" ]; then
    usage "need a command to run" >&2
    exit 2
fi

if ! which xauth >/dev/null; then
    error "xauth command not found"
    exit 3
fi

# If the user did not specify an X authorization file to use, set up a temporary
# directory to house one.
if [ -z "$AUTHFILE" ]; then
    XVFB_RUN_TMPDIR="$(mktemp -d -t $PROGNAME.XXXXXX)"
    # Create empty file to avoid xauth warning
    AUTHFILE=$(mktemp "$XVFB_RUN_TMPDIR/Xauthority")
fi

# Start Xvfb.
# Get the cookie to use.
set +e
MCOOKIE=$(mcookie 2>/dev/null)

# If the mcookie utility is not installed, simulate it.

if [ $? -ne 0 ]; then
   #
   # Set the random device to /dev/random if you need very secure
   # random numbers. Otherwise, /dev/urandom will be fine.
   #

   RANDOM_DEVICE=/dev/urandom

   MCOOKIE=$(od -X -A n -N 16 $RANDOM_DEVICE | tr -d '\011\040')
fi
set -e
tries=10
while [ $tries -gt 0 ]; do
    tries=$(( $tries - 1 ))
    XAUTHORITY=$AUTHFILE xauth source - << EOF >>"$ERRORFILE" 2>&1
add :$SERVERNUM $XAUTHPROTO $MCOOKIE
EOF
    # handle SIGUSR1 so Xvfb knows to send a signal when it's ready to accept
    # connections
    trap : USR1
    (trap '' USR1; exec Xvfb ":$SERVERNUM" $XVFBARGS $LISTENTCP -auth $AUTHFILE >>"$ERRORFILE" 2>&1) &
    XVFBPID=$!

    wait || :
    if kill -0 $XVFBPID 2>/dev/null; then
        break
    elif [ -n "$AUTONUM" ]; then
        # The display is in use so try another one (if '-a' was specified).
        find_free_servernum
        continue
    fi
    error "Xvfb failed to start" >&2
    XVFBPID=
    exit 1
done

PID=$$
PARENT_PID=$(ps -f|awk '$2=='$PID'{print $3 }')

# Start the command and save its exit status.
set +e
DISPLAY=:$SERVERNUM yarn start &
PROCESS_PID=$!

while [ true ]; do
    echo 'ssss'
    if ! kill -0 $PARENT_PID > /dev/null 2>&1; then
        kill -9 $PROCESS_PID        
        exit 0
    fi
    
    sleep 3
done

set -e

# Return the executed command's exit status.
exit 0

# vim:set ai et sts=4 sw=4 tw=80:
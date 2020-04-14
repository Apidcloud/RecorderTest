function handleDatabase()
{
    console.log("Handle Database function called from Unity successfully");
    // You can see the following information from
    // Browser > Inspector > Application tab > Storage > IndexedDB
    let openRequest = indexedDB.open("/idbfs", 21);

    openRequest.onupgradeneeded = function() {
        console.log("upgrade needed");
        // triggers if the client had no database
        // ...perform initialization...
    };

    openRequest.onerror = function() {
        console.error("Error", openRequest.error);
    };

    openRequest.onsuccess = function() {
        let db = openRequest.result;
        console.log(db);

        let store = db.transaction(['FILE_DATA'],'readwrite').objectStore('FILE_DATA');

        var request = store.openCursor();

        var mainKey = null;

        var count = 0;

        request.onsuccess = async function(event) {
            var cursor = event.target.result;

            if (cursor) {
            
            if (mainKey == null){
                mainKey = cursor.key;
                console.log("setting main base key as: " + cursor.key);
            }

            var searchString = mainKey + "/";

            var reg = new RegExp('^' + searchString.replace(/[\/]/g, '\\$&') + '\\d\\d\\d\\d\\.jpg$');
            //console.log(reg);
            var testRegex = reg.test(cursor.key);
            //console.log(testTrue);

            if (testRegex){
                console.log("Found image key: " + cursor.key);
                // just a test with the first retrieved image to check if it works
                if (count === 0){
                    getAndSetImage(cursor.key);
                    count++;
                }
            }

            cursor.continue();
            }
        };
        
        getAndSetImage = (key) => {
            //document.querySelector(".webgl-content").style.visibility = "hidden";
            var image = document.querySelector("#testImage");

            let req = store.get(key);
            req.onsuccess = function(e) {
                let record = e.target.result;

                //var blob = new Blob( [ record.contents ], { type: "image/jpeg" } );
                //var urlCreator = window.URL || window.webkitURL;
                //console.log(urlCreator);
                //var imageUrl = URL.createObjectURL(blob);
                //console.log(imageUrl);
                var binary = "";
                console.log(record.contents.length);
                for (var i = 0; i < record.contents.length; i++){
                    binary += String.fromCharCode(record.contents[i]);
                }
                    
                var base64 = b2a(binary);

                var imageSrc = 'data:image/jpeg;base64,' + base64;
                //var imageSrc = "example2.png";
                
                //console.log(imageSrc);
                image.src = imageSrc;
                
                // puppeteer related
            
                var div = document.querySelector("#anotherTest");
                div.style.width = "100px";
                div.style.height = "100px";
                div.style.background = "red";
                div.style.color = "white";
                div.innerHTML = "Hello";

                var eventImage = document.createElement("div");
                eventImage.setAttribute("id", "imageready");
                document.body.appendChild(eventImage);
            };
        };

        function b2a(a) {
            var c, d, e, f, g, h, i, j, o, b = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=", k = 0, l = 0, m = "", n = [];
            if (!a) return a;
            do c = a.charCodeAt(k++), d = a.charCodeAt(k++), e = a.charCodeAt(k++), j = c << 16 | d << 8 | e, 
            f = 63 & j >> 18, g = 63 & j >> 12, h = 63 & j >> 6, i = 63 & j, n[l++] = b.charAt(f) + b.charAt(g) + b.charAt(h) + b.charAt(i); while (k < a.length);
            return m = n.join(""), o = a.length % 3, (o ? m.slice(0, o - 3) :m) + "===".slice(o || 3);
          }
        
    };
}
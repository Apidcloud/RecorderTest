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

        request.onsuccess = function(event) {
            var cursor = event.target.result;

            if (cursor) {
            
            if (mainKey == null){
                mainKey = cursor.key;
                console.log("setting main base key as: " + cursor.key);
            }

            var searchString = mainKey + "/";

            var reg = new RegExp('^' + searchString.replace(/[\/]/g, '\\$&') + '\\d\\d\\d\\d\\.jpg$');

            if (reg.test(cursor.key)){
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
        let image = document.querySelector('#testImage');

        let req = store.get(key);
        req.onsuccess = function(e) {
            let record = e.target.result;
            var blob = new Blob( [ record.contents ], { type: "image/jpeg" } );
            var urlCreator = window.URL || window.webkitURL;
            var imageUrl = urlCreator.createObjectURL(blob);
            image.src = imageUrl;
        };
        };
        
    };
}
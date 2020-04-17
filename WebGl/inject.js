var fs = require('fs');
var path = require('path');
var filePath = path.join(__dirname, 'Build/index.html');

const encoding = {encoding: 'utf-8'};
let content = fs.readFileSync(filePath, encoding).toString().split(/\r?\n/);

const unityLoaderScript = '<script src="Build/UnityLoader.js"></script>';
const handleDatabaseScriptToInject = '    <script src="../handleDatabase.js"></script>';

const bodyTag = '  <body>';
const imageTagToInject = '    <img id="testImage">';

injectCodeOnce = (contentArray, afterString, codeToInject) =>{
    let newContent = contentArray;
    for (let i = 0; i < contentArray.length; i++) {
        if (contentArray[i].includes(afterString)){
            newContent.splice(i+1, 0, codeToInject);
            break;
        }
    }
    return newContent;
}

// inject handle database script, which is called from unity
content = injectCodeOnce(content, unityLoaderScript, handleDatabaseScriptToInject);

// inject test image tag which will show the first captured frame in the page
content = injectCodeOnce(content, bodyTag, imageTagToInject);

//console.log(content);

// overwrite file with injected code
fs.writeFileSync(filePath, content.join('\r\n'));

//content = fs.readFileSync(filePath, encoding).toString().split(/\r?\n/);
//console.log(content);





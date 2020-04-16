const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({headless: false,
        args:[
            //'--headless',
            '--hide-scrollbars',
            '--mute-audio',
            '--no-sandbox',
            //'--use-gl=swiftshader'
         ]});
    const page = await browser.newPage();

    /*
    await page
        .goto('chrome://gpu', { waitUntil: 'networkidle0', timeout: 20 * 60 * 1000 })
        .catch(e => console.log(e)); 
    await page.screenshot({
        path: 'gpu_stats_swift.png'
    });
    await browser.close();*/

    // Create raw protocol session.
    //const session = await page.target().createCDPSession();
    //const {windowId} = await session.send('Browser.getWindowForTarget');
    //await session.send('Browser.setWindowBounds', {windowId, bounds: {windowState: 'minimized'}});

    await page.setViewport({
        width: 1920,
        height: 1080,
        deviceScaleFactor: 1,
    });
    
    page.on('console', msg => console.log('PAGE LOG:', msg.text()));

    /*
    const webgl = await page.evaluate(() => {
        const canvas = document.createElement('canvas');
        const gl = canvas.getContext('webgl');
        const expGl = canvas.getContext('experimental-webgl');
    
        return {
          gl: gl && gl instanceof WebGLRenderingContext,
          expGl: expGl && expGl instanceof WebGLRenderingContext,
        };
      });
    
      console.log('WebGL Support:', webgl); */

    //await page.setCacheEnabled(false);
    await page.goto('http://localhost:8080/Build/');

    await page.screenshot({path: 'example2.png'});
    //await session.send('Browser.setWindowBounds', {windowId, bounds: {windowState: 'minimized'}});
    await page.waitForSelector("#imageready");
    await page.screenshot({path: 'example.png'});
    //await session.send('Browser.setWindowBounds', {windowId, bounds: {windowState: 'minimized'}});
    await browser.close();
})();
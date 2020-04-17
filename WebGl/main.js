const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({
        // to run with sound, since chromium has some problem decoding it
        // but firefox does not support webgl headlessly
        // to install puppeteer with firefox, remove the current with '$ yarn remove puppetteer'
        // and run '$ PUPPETEER_PRODUCT=firefox yarn add puppeteer'

        //product: 'firefox', // if installed through puppetteer
        // another alternative, is to use your own browser by providing its path
        // executablePath: '/path/to/Chrome',
        headless: false,
        args:[
            //'--foreground' //firefox flag
            //'--headless',
            //'--hide-scrollbars',
            //'--mute-audio',
            '--no-sandbox',
            '--use-gl=swiftshader',
            //'--disable-gpu'
         ],
         ignoreDefaultArgs: ["--mute-audio", "--hide-scrollbars", "--headless"]
        });
    const page = await browser.newPage();

    /*
    await page
        .goto('chrome://gpu', { waitUntil: 'networkidle0', timeout: 20 * 60 * 1000 })
        .catch(e => console.log(e)); 
    await page.screenshot({
        path: 'GPU_stats/gpu_stats_swiftshader_headless_swiftshader_no_gpu.png'
    });
    await browser.close();*/

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
    await page.screenshot({path: 'ScreenshotResults/beforeLoading.png'});
    await page.waitForSelector("#imageready");
    await page.screenshot({path: 'ScreenshotResults/renderingResult.png'});

    // doesn't seem to kill the process properly in headless mode
    await browser.close();
})();
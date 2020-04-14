const puppeteer = require('puppeteer');

(async () => {
    const browser = await puppeteer.launch({headless: false});
    const page = await browser.newPage();
    await page.setViewport({
        width: 1920,
        height: 1080,
        deviceScaleFactor: 1,
    });
    
    page.on('console', msg => console.log('PAGE LOG:', msg.text()));

    await page.goto('http://localhost:8080/WebGl/Build/');

    await page.screenshot({path: 'example2.png'});
    await page.waitForSelector("#imageready");
    await page.screenshot({path: 'example.png'});
    //await browser.close();
})();
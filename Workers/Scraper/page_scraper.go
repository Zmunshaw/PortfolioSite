package main

func scrapePage() {
	browser, err = m.PW.Chromium.Launch(playwright.BrowserTypeLaunchOptions{
		Args: []string{"--disable-blink-features=AutomationControlled"},
	})
}

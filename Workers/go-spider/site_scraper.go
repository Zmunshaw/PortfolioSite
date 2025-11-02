package main

import (
	"encoding/json"
	"os"
	"os/exec"
	"path/filepath"
)

// Calls a python script since go is garbo for browser rendering and headless does not get content
func ScrapeSites(urls []string) ([]map[string]interface{}, error) {
	jsonBytes, err := json.Marshal(urls)
	if err != nil {
		return nil, err
	}

	wd, err := os.Getwd()
	if err != nil {
		return nil, err
	}

	pythonPath := filepath.Join(wd, ".venv", "bin", "python3")

	cmd := exec.Command(pythonPath, "stealth_scrape.py", string(jsonBytes))
	output, err := cmd.CombinedOutput()

	if err != nil {
		return nil, err
	}

	var results []map[string]interface{}
	err = json.Unmarshal(output, &results)
	if err != nil {
		return nil, err
	}

	return results, nil
}

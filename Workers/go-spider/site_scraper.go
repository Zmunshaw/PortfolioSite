package main

import (
	"bytes"
	"encoding/json"
	"fmt"
	"os/exec"
)

func ScrapeSites(urls []string) ([]map[string]interface{}, error) {
	jsonBytes, err := json.Marshal(urls)
	if err != nil {
		return nil, err
	}

	venvPath := "./venv/bin/python3"
	cmd := exec.Command(venvPath, "python/stealth_scrape.py", string(jsonBytes))

	var out bytes.Buffer
	cmd.Stdout = &out
	cmd.Stderr = &out

	err = cmd.Run()
	if err != nil {
		fmt.Printf("Python error: %s\n", out.String())
		return nil, err
	}

	var results []map[string]interface{}
	err = json.Unmarshal(out.Bytes(), &results)
	if err != nil {
		fmt.Printf("JSON parse error: %s\n", out.String())
		return nil, err
	}

	return results, nil
}

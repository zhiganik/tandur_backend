Deploy the latest code to the production server.

Follow these steps in order:

**Step 1 — Pull latest code on the server**
Run:
```
ssh tandur "cd /home/deploy/app && git pull"
```
Show the git pull output. If it says "Already up to date", confirm with the user before continuing — they may have forgotten to push.

**Step 2 — Rebuild and restart containers**
Run:
```
ssh tandur "cd /home/deploy/app && docker compose up -d --build"
```
This rebuilds the API image and restarts all services. Show the output.

**Step 3 — Confirm**
After both commands succeed, tell the user the deployment is complete and which commit is now live (from the git pull output).

If either command fails, show the error clearly and stop — do not continue to the next step.

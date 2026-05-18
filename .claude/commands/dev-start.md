Start the local development environment for this project.

Follow these steps in order:

**Step 1 — Check for running containers**
Run `docker compose ps` to see if any services are currently up.
If any containers are running, run `docker compose down` to stop them cleanly before starting fresh.

**Step 2 — Start infrastructure**
Run:
```
docker compose up -d postgres redis seq
```
Wait for the command to finish and confirm all three containers started successfully.

**Step 3 — Start the API**
Run `dotnet run --project src/Api` in the background so the terminal stays free.

**Step 4 — Report URLs**
Tell the user the following are now available:
- Swagger UI: http://localhost:5280/api/swagger
- Seq logs:   http://localhost:5341
- Postgres:   localhost:5433
- Redis:      localhost:6379

Remind them to press Ctrl+C in their terminal to stop the API when done.

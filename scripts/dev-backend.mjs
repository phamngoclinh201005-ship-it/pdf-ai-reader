import { readFileSync } from 'node:fs'
import { spawn } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const project = fileURLToPath(new URL('../backend/PaperFlow.Api/', import.meta.url))
const config = JSON.parse(readFileSync(new URL('../backend/PaperFlow.Api/appsettings.json', import.meta.url), 'utf8'))
const connection = config.MongoDB?.ConnectionString
if (!/^mongodb:\/\/(localhost|127\.0\.0\.1)(:|\/)/.test(connection || '')) {
  throw new Error('Local development requires a localhost MongoDB connection in appsettings.json.')
}
// Environment overrides User Secrets for this child only; Atlas secrets remain intact.
const child = spawn('dotnet', ['run', '--launch-profile', 'http'], {
  cwd: project,
  stdio: 'inherit',
  env: { ...process.env, MongoDB__ConnectionString: connection },
})
child.on('error', error => { console.error(error.message); process.exitCode = 1 })
child.on('exit', code => { process.exitCode = code ?? 1 })

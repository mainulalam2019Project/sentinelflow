import signalR from '@microsoft/signalr';
const { HubConnectionBuilder } = signalR;

const login = await fetch('http://localhost:5245/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'admin@sentinelflow.dev', password: 'Admin@123' })
});
const { accessToken } = await login.json();

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5245/hubs/metrics', { accessTokenFactory: () => accessToken })
  .build();

connection.on('MetricUpdated', (m) => console.log('GOT', JSON.stringify(m)));
connection.onclose((e) => console.log('CLOSED', e?.message));
await connection.start();
console.log('connected');
await new Promise((r) => setTimeout(r, 30000));
console.log('done');
process.exit(0);

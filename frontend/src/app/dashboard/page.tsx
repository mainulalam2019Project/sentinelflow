"use client";
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { API_URL, apiFetch, getToken } from "@/lib/api";
import { LineChart, Line, XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid } from "recharts";

interface Service { id: string; name: string; health: string; slaTarget: number }
interface LatencyPoint { time: string; latency: number; errors: number }

export default function DashboardPage() {
  const [services, setServices] = useState<Service[]>([]);
  const [incidents, setIncidents] = useState<{ total: number }>({ total: 0 });
  const [series, setSeries] = useState<LatencyPoint[]>([]);
  const [eventCount, setEventCount] = useState(0);
  const [connected, setConnected] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => setMounted(true), []);

  useEffect(() => {
    const token = getToken();
    apiFetch("/api/services", token).then((d) => setServices(d.items)).catch(() => {});
    apiFetch("/api/incidents?status=Open", token).then((d) => setIncidents({ total: d.total })).catch(() => {});
  }, []);

  useEffect(() => {
    const token = getToken();
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/metrics`, { accessTokenFactory: () => token ?? "" })
      .withAutomaticReconnect()
      .build();

    connection.on("MetricUpdated", (metric: { serviceId: string; type: string; value: number; timestamp: string }) => {
      (window as unknown as { __events: unknown[] }).__events = [...((window as unknown as { __events?: unknown[] }).__events ?? []), metric];
      setEventCount((c) => c + 1);
      if (metric.type === "Latency" || metric.type === "ErrorRate") {
        setSeries((prev) => {
          const time = new Date(metric.timestamp).toLocaleTimeString();
          const existingIndex = prev.findIndex((p) => p.time === time);
          const latency = metric.type === "Latency" ? Math.round(metric.value) : 0;
          const errors = metric.type === "ErrorRate" ? Math.round(metric.value * 10) / 10 : 0;
          if (existingIndex >= 0) {
            const updated = prev.map((p, i) =>
              i === existingIndex
                ? { ...p, latency: latency > 0 ? latency : p.latency, errors: errors > 0 ? errors : p.errors }
                : p
            );
            return updated;
          }
          return [...prev.slice(-29), { time, latency, errors }];
        });
      }
    });

    connection.start().then(() => setConnected(true)).catch(() => setConnected(false));
    return () => { connection.stop(); };
  }, []);

  if (!mounted) return null;
  if (!getToken()) {
    return <p className="text-slate-400">Please <a className="text-blue-400 underline" href="/login">log in</a>.</p>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Operations Overview</h1>
        <span className={`rounded-full px-3 py-1 text-xs ${connected ? "bg-emerald-950 text-emerald-300" : "bg-slate-800 text-slate-400"}`}>
          {connected ? `Live stream connected (${eventCount} events)` : "Connecting..."}
        </span>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <Stat label="Services" value={services.length} />
        <Stat label="Open Incidents" value={incidents.total} />
        <Stat label="Healthy" value={services.filter((s) => s.health === "Healthy").length} />
        <Stat label="Unhealthy" value={services.filter((s) => s.health === "Unhealthy").length} />
      </div>

      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-4 font-semibold">Live Latency (ms)</h2>
        <ResponsiveContainer width="100%" height={260}>
          <LineChart data={series}>
            <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
            <XAxis dataKey="time" stroke="#475569" fontSize={11} />
            <YAxis stroke="#475569" fontSize={11} />
            <Tooltip contentStyle={{ background: "#0f172a", border: "1px solid #1e293b" }} />
            <Line type="monotone" dataKey="latency" stroke="#3b82f6" dot={false} strokeWidth={2} />
            <Line type="monotone" dataKey="errors" stroke="#f43f5e" dot={false} strokeWidth={2} />
          </LineChart>
        </ResponsiveContainer>
      </div>

      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 font-semibold">Service Status</h2>
        <table className="w-full text-sm">
          <thead><tr className="text-left text-slate-400"><th className="pb-2">Name</th><th>Health</th><th>SLA target</th></tr></thead>
          <tbody>
            {services.map((s) => (
              <tr key={s.id} className="border-t border-slate-800">
                <td className="py-2">{s.name}</td>
                <td><span className={`rounded px-2 py-0.5 text-xs ${s.health === "Healthy" ? "bg-emerald-950 text-emerald-300" : s.health === "Degraded" ? "bg-amber-950 text-amber-300" : "bg-red-950 text-red-300"}`}>{s.health}</span></td>
                <td>{s.slaTarget}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: number | string }) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
      <p className="text-2xl font-bold">{value}</p>
      <p className="text-xs text-slate-400">{label}</p>
    </div>
  );
}

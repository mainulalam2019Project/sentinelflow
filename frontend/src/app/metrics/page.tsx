"use client";
import { useEffect, useState } from "react";
import { apiFetch, getToken } from "@/lib/api";

export default function MetricsPage() {
  const [services, setServices] = useState<Array<{ id: string; name: string }>>([]);
  const [serviceId, setServiceId] = useState("");
  const [metrics, setMetrics] = useState<Array<{ type: string; value: number; timestamp: string }>>([]);

  useEffect(() => {
    apiFetch("/api/services", getToken()).then((d) => { setServices(d.items); if (d.items[0]) setServiceId(d.items[0].id); });
  }, []);

  useEffect(() => {
    if (!serviceId) return;
    apiFetch(`/api/metrics/${serviceId}?hours=2`, getToken()).then(setMetrics).catch(() => {});
  }, [serviceId]);

  const byType = (t: string) => {
    const arr = metrics.filter((m) => m.type === t);
    return arr.length ? Math.round(arr.reduce((s, m) => s + m.value, 0) / arr.length * 10) / 10 : 0;
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Metrics</h1>
      <select value={serviceId} onChange={(e) => setServiceId(e.target.value)} className="rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm">
        {services.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
      </select>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Stat label="Avg Latency" value={`${byType("Latency")}ms`} />
        <Stat label="Avg Error Rate" value={`${byType("ErrorRate")}%`} />
        <Stat label="Avg CPU" value={`${byType("CpuUsage")}%`} />
        <Stat label="Avg Memory" value={`${byType("MemoryUsage")}%`} />
      </div>
      <p className="text-sm text-slate-500">{metrics.length} samples in the last 2 hours</p>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return <div className="rounded-xl border border-slate-800 bg-slate-900 p-4"><p className="text-xl font-bold">{value}</p><p className="text-xs text-slate-400">{label}</p></div>;
}

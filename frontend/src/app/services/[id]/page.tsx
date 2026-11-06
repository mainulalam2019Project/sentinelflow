"use client";
import { use, useEffect, useState } from "react";
import { apiFetch, getToken } from "@/lib/api";

export default function ServiceDetail({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [service, setService] = useState<{ name: string; health: string; slaTarget: number; owner: string; baseUrl: string; environment: string } | null>(null);
  const [metrics, setMetrics] = useState<Array<{ type: string; value: number; timestamp: string }>>([]);

  useEffect(() => {
    const token = getToken();
    apiFetch(`/api/services/${id}`, token).then(setService).catch(() => {});
    apiFetch(`/api/metrics/${id}?hours=2`, token).then(setMetrics).catch(() => {});
  }, [id]);

  if (!service) return <p className="text-slate-400">Loading...</p>;

  const latencies = metrics.filter((m) => m.type === "Latency");
  const avg = latencies.length ? Math.round(latencies.reduce((s, m) => s + m.value, 0) / latencies.length) : 0;
  const errs = metrics.filter((m) => m.type === "ErrorRate");
  const maxErr = errs.length ? Math.max(...errs.map((e) => e.value)) : 0;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">{service.name}</h1>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <Stat label="Health" value={service.health} />
        <Stat label="Avg Latency" value={`${avg}ms`} />
        <Stat label="Peak Error Rate" value={`${maxErr}%`} />
        <Stat label="SLA Target" value={`${service.slaTarget}%`} />
      </div>
      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 font-semibold">Details</h2>
        <dl className="grid grid-cols-2 gap-2 text-sm">
          <dt className="text-slate-400">Owner</dt><dd>{service.owner}</dd>
          <dt className="text-slate-400">Environment</dt><dd>{service.environment}</dd>
          <dt className="text-slate-400">Base URL</dt><dd className="truncate">{service.baseUrl}</dd>
        </dl>
      </div>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return <div className="rounded-xl border border-slate-800 bg-slate-900 p-4"><p className="text-xl font-bold">{value}</p><p className="text-xs text-slate-400">{label}</p></div>;
}

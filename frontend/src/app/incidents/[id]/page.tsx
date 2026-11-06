"use client";
import { use, useEffect, useState } from "react";
import { apiFetch, getToken, API_URL } from "@/lib/api";

export default function IncidentDetail({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const [incident, setIncident] = useState<{
    incidentNumber: string; title: string; service: string; severity: string; status: string; summary: string;
    probableRootCause: string | null; rootCauseConfidence: number | null; createdAt: string; resolvedAt: string | null;
    events: Array<{ message: string; occurredAt: string }>;
  } | null>(null);

  const load = () => apiFetch(`/api/incidents/${id}`, getToken()).then(setIncident).catch(() => {});

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { load(); }, [id]);

  const action = async (path: string) => {
    const token = getToken();
    await fetch(`${API_URL}${path}`, { method: "POST", headers: { Authorization: `Bearer ${token}` } });
    load();
  };

  if (!incident) return <p className="text-slate-400">Loading...</p>;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{incident.incidentNumber}</h1>
        <span className="rounded bg-slate-800 px-3 py-1 text-xs">{incident.severity} · {incident.status}</span>
      </div>
      <p className="text-slate-300">{incident.title}</p>
      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-2 font-semibold">AI Incident Summary</h2>
        <p className="text-sm text-slate-400">{incident.summary}</p>
        {incident.probableRootCause && <p className="mt-3 text-sm">Probable root cause: <b>{incident.probableRootCause}</b> ({Math.round((incident.rootCauseConfidence ?? 0) * 100)}% confidence)</p>}
      </div>
      <div className="flex gap-3">
        <button onClick={() => action(`/api/incidents/${id}/acknowledge`)} className="rounded-lg border border-slate-700 px-4 py-2 text-sm hover:bg-slate-800">Acknowledge</button>
        <button onClick={() => action(`/api/incidents/${id}/resolve`)} className="rounded-lg bg-emerald-700 px-4 py-2 text-sm hover:bg-emerald-600">Resolve</button>
        <button onClick={() => action(`/api/remediations/${id}/approve`)} className="rounded-lg bg-blue-700 px-4 py-2 text-sm hover:bg-blue-600">Approve Remediation</button>
      </div>
      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4">
        <h2 className="mb-3 font-semibold">Timeline</h2>
        <ol className="space-y-2 text-sm">
          {incident.events.map((e, i) => (
            <li key={i} className="flex gap-4 border-b border-slate-800 pb-2">
              <span className="font-mono text-slate-500">{new Date(e.occurredAt).toLocaleTimeString()}</span>
              <span>{e.message}</span>
            </li>
          ))}
        </ol>
      </div>
    </div>
  );
}

"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { apiFetch, getToken } from "@/lib/api";

export default function IncidentsPage() {
  const [incidents, setIncidents] = useState<Array<{ id: string; incidentNumber: string; service: string; severity: string; status: string; title: string; createdAt: string }>>([]);

  useEffect(() => {
    apiFetch("/api/incidents", getToken()).then((d) => setIncidents(d.items)).catch(() => {});
  }, []);

  if (!getToken()) return <p className="text-slate-400">Please <a className="text-blue-400 underline" href="/login">log in</a>.</p>;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Incidents</h1>
      {incidents.length === 0 && <p className="text-slate-500">No incidents. The system is healthy.</p>}
      <div className="space-y-3">
        {incidents.map((i) => (
          <Link key={i.id} href={`/incidents/${i.id}`} className="block rounded-xl border border-slate-800 bg-slate-900 p-4 hover:border-slate-700">
            <div className="flex items-center justify-between">
              <p className="font-semibold">{i.title}</p>
              <span className={`rounded px-2 py-0.5 text-xs ${i.severity === "Critical" ? "bg-red-950 text-red-300" : i.severity === "High" ? "bg-orange-950 text-orange-300" : "bg-amber-950 text-amber-300"}`}>{i.severity}</span>
            </div>
            <p className="text-sm text-slate-400">{i.incidentNumber} · {i.service} · {new Date(i.createdAt).toLocaleString()}</p>
            <p className="mt-1 text-xs text-slate-500">{i.status}</p>
          </Link>
        ))}
      </div>
    </div>
  );
}

"use client";
import { useEffect, useState } from "react";
import Link from "next/link";
import { apiFetch, getToken } from "@/lib/api";

export default function ServicesPage() {
  const [services, setServices] = useState<Array<{ id: string; name: string; environment: string; owner: string; criticality: string; slaTarget: number; health: string }>>([]);

  useEffect(() => {
    apiFetch("/api/services", getToken()).then((d) => setServices(d.items)).catch(() => {});
  }, []);

  if (!getToken()) return <p className="text-slate-400">Please <a className="text-blue-400 underline" href="/login">log in</a>.</p>;

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Services</h1>
      <table className="w-full text-sm">
        <thead><tr className="text-left text-slate-400"><th className="pb-2">Name</th><th>Environment</th><th>Owner</th><th>Criticality</th><th>SLA</th><th>Health</th></tr></thead>
        <tbody>
          {services.map((s) => (
            <tr key={s.id} className="border-t border-slate-800 hover:bg-slate-900">
              <td className="py-3"><Link className="text-blue-400 hover:underline" href={`/services/${s.id}`}>{s.name}</Link></td>
              <td>{s.environment}</td>
              <td>{s.owner}</td>
              <td>{s.criticality}</td>
              <td>{s.slaTarget}%</td>
              <td><span className={`rounded px-2 py-0.5 text-xs ${s.health === "Healthy" ? "bg-emerald-950 text-emerald-300" : s.health === "Unhealthy" ? "bg-red-950 text-red-300" : "bg-amber-950 text-amber-300"}`}>{s.health}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

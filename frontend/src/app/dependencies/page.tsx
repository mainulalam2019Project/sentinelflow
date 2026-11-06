"use client";
import { useEffect, useState } from "react";
import { apiFetch, getToken } from "@/lib/api";

export default function DependenciesPage() {
  const [deps, setDeps] = useState<Array<{ id: string; service: string; dependsOn: string }>>([]);

  useEffect(() => {
    apiFetch("/api/dependencies", getToken()).then(setDeps).catch(() => {});
  }, []);

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Dependency Graph</h1>
      {deps.length === 0 && <p className="text-slate-500">No dependencies registered.</p>}
      <ul className="space-y-2">
        {deps.map((d) => (
          <li key={d.id} className="flex items-center gap-3 rounded-lg border border-slate-800 bg-slate-900 p-3 text-sm">
            <b>{d.service}</b>
            <span className="text-slate-500">depends on</span>
            <b className="text-blue-400">{d.dependsOn}</b>
          </li>
        ))}
      </ul>
    </div>
  );
}

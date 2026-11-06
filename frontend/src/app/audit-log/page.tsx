"use client";
import { useEffect, useState } from "react";
import { apiFetch, getToken } from "@/lib/api";

export default function AuditLogPage() {
  const [logs, setLogs] = useState<Array<{ id: string; action: string; entityType: string | null; details: string | null; timestamp: string }>>([]);

  useEffect(() => {
    apiFetch("/api/audit-logs", getToken()).then(setLogs).catch(() => {});
  }, []);

  return (
    <div>
      <h1 className="mb-6 text-2xl font-bold">Audit Log</h1>
      <table className="w-full text-sm">
        <thead><tr className="text-left text-slate-400"><th className="pb-2">Action</th><th>Entity</th><th>Details</th><th>Timestamp</th></tr></thead>
        <tbody>
          {logs.map((l) => (
            <tr key={l.id} className="border-t border-slate-800">
              <td className="py-2">{l.action}</td>
              <td>{l.entityType ?? "—"}</td>
              <td className="max-w-xs truncate">{l.details ?? "—"}</td>
              <td>{new Date(l.timestamp).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

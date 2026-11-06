"use client";
import { useRouter } from "next/navigation";
export default function SettingsPage() {
  const router = useRouter();
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Settings</h1>
      <div className="rounded-xl border border-slate-800 bg-slate-900 p-4 text-sm text-slate-400">
        <p>API base URL: {process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5245"}</p>
        <p className="mt-2">Theme: dark · Role-based access enforced server-side</p>
      </div>
      <button onClick={() => { localStorage.removeItem("sf_token"); router.push("/login"); }} className="rounded-lg border border-slate-700 px-4 py-2 text-sm hover:bg-slate-800">Sign out</button>
    </div>
  );
}

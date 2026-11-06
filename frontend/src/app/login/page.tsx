"use client";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { API_URL } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const submit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    const data = new FormData(e.currentTarget);
    try {
      const res = await fetch(`${API_URL}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email: data.get("email"), password: data.get("password") }),
      });
      if (!res.ok) { setError("Invalid credentials"); return; }
      const json = await res.json();
      localStorage.setItem("sf_token", json.accessToken);
      router.push("/dashboard");
    } catch {
      setError("Cannot reach API server");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center">
      <form onSubmit={submit} className="w-full max-w-sm rounded-xl border border-slate-800 bg-slate-900 p-8">
        <h1 className="text-xl font-bold text-blue-400">SentinelFlow</h1>
        <p className="mb-6 text-sm text-slate-400">Sign in to your engineering dashboard</p>
        {error && <p className="mb-3 rounded bg-red-950 px-3 py-2 text-sm text-red-300">{error}</p>}
        <label className="mb-1 block text-sm">Email</label>
        <input name="email" type="email" required defaultValue="admin@sentinelflow.dev" className="mb-4 w-full rounded-lg border border-slate-700 bg-transparent px-3 py-2" />
        <label className="mb-1 block text-sm">Password</label>
        <input name="password" type="password" required defaultValue="Admin@123" className="mb-6 w-full rounded-lg border border-slate-700 bg-transparent px-3 py-2" />
        <button disabled={loading} className="w-full rounded-lg bg-blue-600 py-2 font-semibold hover:bg-blue-500 disabled:opacity-60">
          {loading ? "Signing in..." : "Sign in"}
        </button>
        <p className="mt-4 text-xs text-slate-500">Demo: admin@sentinelflow.dev / Admin@123</p>
      </form>
    </div>
  );
}

"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { Activity, ShieldAlert, GitBranch, FileClock, Settings, Server, AreaChart } from "lucide-react";

const links = [
  { href: "/dashboard", label: "Dashboard", icon: Activity },
  { href: "/services", label: "Services", icon: Server },
  { href: "/incidents", label: "Incidents", icon: ShieldAlert },
  { href: "/dependencies", label: "Dependencies", icon: GitBranch },
  { href: "/metrics", label: "Metrics", icon: AreaChart },
  { href: "/audit-log", label: "Audit Log", icon: FileClock },
  { href: "/settings", label: "Settings", icon: Settings },
];

export function Shell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  if (pathname === "/login") return <>{children}</>;
  return (
    <div className="flex min-h-screen">
      <aside className="w-56 border-r border-slate-800 p-4">
        <p className="mb-6 text-lg font-bold text-blue-400">SentinelFlow</p>
        <nav className="flex flex-col gap-1">
          {links.map(({ href, label, icon: Icon }) => (
            <Link key={href} href={href} className={`flex items-center gap-2 rounded-lg px-3 py-2 text-sm ${pathname?.startsWith(href) ? "bg-slate-800 text-blue-300" : "text-slate-400 hover:bg-slate-900"}`}>
              <Icon className="h-4 w-4" /> {label}
            </Link>
          ))}
        </nav>
      </aside>
      <main className="min-w-0 flex-1 p-6">{children}</main>
    </div>
  );
}

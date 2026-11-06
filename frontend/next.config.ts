import type { NextConfig } from "next";
const nextConfig: NextConfig = { turbopack: { root: __dirname }, agentRules: false } as NextConfig;
export default nextConfig;

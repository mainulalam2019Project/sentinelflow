import type { Config } from "tailwindcss";
const config: Config = {
  content: ["./src/**/*.{ts,tsx}"],
  theme: { extend: { colors: { brand: { 600: "#2563eb", 500: "#3b82f6", 400: "#60a5fa" } } } },
  plugins: [],
};
export default config;

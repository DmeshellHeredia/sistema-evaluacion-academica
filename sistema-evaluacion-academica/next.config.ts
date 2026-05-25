import type { NextConfig } from "next";
import path from "path";

const isDev = process.env.NODE_ENV === "development";
const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

// Development: 'unsafe-eval' para HMR, ws:/wss: para hot-reload WebSocket.
// Production: sin 'unsafe-eval'; 'unsafe-inline' requerido por Next.js App Router
//             (inyecta scripts inline para RSC payload — nonces requieren servidor).
const csp = isDev
  ? [
      "default-src 'self'",
      "script-src 'self' 'unsafe-inline' 'unsafe-eval'",
      "style-src 'self' 'unsafe-inline'",
      "font-src 'self'",
      "img-src 'self' data: blob:",
      `connect-src 'self' ${apiUrl} ws: wss:`,
      "frame-ancestors 'none'",
      "object-src 'none'",
      "base-uri 'self'",
    ].join("; ")
  : [
      "default-src 'self'",
      "script-src 'self' 'unsafe-inline'",
      "style-src 'self' 'unsafe-inline'",
      "font-src 'self'",
      "img-src 'self' data: blob:",
      `connect-src 'self' ${apiUrl}`,
      "frame-ancestors 'none'",
      "object-src 'none'",
      "base-uri 'self'",
    ].join("; ");

const nextConfig: NextConfig = {
  turbopack: {
    root: path.resolve(__dirname),
  },
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [{ key: "Content-Security-Policy", value: csp }],
      },
    ];
  },
};

export default nextConfig;

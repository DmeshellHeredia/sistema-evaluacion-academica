import type { Metadata } from "next"
import { Poppins } from "next/font/google"
import "./globals.css"
import { Providers } from "@/components/providers"

const poppins = Poppins({
  variable: "--font-sans",
  subsets: ["latin"],
  weight: ["400", "500", "600", "700"],
})

export const metadata: Metadata = {
  title: "Academia — Sistema de Evaluación",
  description: "Dashboard académico para gestión de estudiantes, materias y calificaciones",
}

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es" className={`${poppins.variable} h-full`}>
      <body className="h-full antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}

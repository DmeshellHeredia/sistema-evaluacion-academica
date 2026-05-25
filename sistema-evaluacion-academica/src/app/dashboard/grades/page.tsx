"use client"

import { useAuth } from "@/context/auth-context"
import { ProfesorGradesView } from "./_components/ProfesorGradesView"
import { AdminGradesView } from "./_components/AdminGradesView"
import { EstudianteGradesView } from "./_components/EstudianteGradesView"

export default function GradesPage() {
  const { user } = useAuth()
  if (user?.role === "Profesor") return <ProfesorGradesView />
  if (user?.role === "Estudiante") return <EstudianteGradesView />
  return <AdminGradesView />
}

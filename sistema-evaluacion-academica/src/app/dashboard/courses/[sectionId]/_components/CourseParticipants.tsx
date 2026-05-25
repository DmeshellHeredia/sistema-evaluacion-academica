"use client"

import { useQuery } from "@tanstack/react-query"
import { coursesApi } from "@/lib/api"
import { Spinner } from "./shared"

export function CourseParticipants({ sectionId }: { sectionId: string }) {
  const { data: participants = [], isLoading } = useQuery({
    queryKey: ["course", sectionId, "participants"],
    queryFn: () => coursesApi.getParticipants(sectionId),
  })

  if (isLoading) return <Spinner />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-foreground">Participantes</h2>
        <span className="text-sm text-muted-foreground">{participants.length} estudiantes</span>
      </div>
      <div className="bg-card border border-border rounded-2xl overflow-hidden">
        <div className="grid grid-cols-[1fr_auto_auto_auto] gap-4 px-5 py-3 border-b border-border text-xs font-semibold text-muted-foreground uppercase tracking-wide">
          <span>Estudiante</span>
          <span className="text-center">Entregas</span>
          <span className="text-center">Calificadas</span>
          <span className="text-center">Promedio</span>
        </div>
        <div className="divide-y divide-border">
          {participants.map((p) => (
            <div key={p.studentId} className="grid grid-cols-[1fr_auto_auto_auto] gap-4 items-center px-5 py-3.5">
              <div>
                <p className="text-sm font-medium text-foreground">{p.fullName}</p>
                <p className="text-xs text-muted-foreground">{p.studentCode}</p>
              </div>
              <span className="text-sm text-center text-foreground">{p.submissionsCount}</span>
              <span className="text-sm text-center text-foreground">{p.gradedCount}</span>
              <span className={`text-sm text-center font-semibold ${p.averageScore !== null ? (p.averageScore >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500") : "text-muted-foreground"}`}>
                {p.averageScore !== null ? p.averageScore.toFixed(1) : "—"}
              </span>
            </div>
          ))}
          {participants.length === 0 && (
            <p className="text-xs text-muted-foreground text-center py-8">Sin participantes</p>
          )}
        </div>
      </div>
    </div>
  )
}

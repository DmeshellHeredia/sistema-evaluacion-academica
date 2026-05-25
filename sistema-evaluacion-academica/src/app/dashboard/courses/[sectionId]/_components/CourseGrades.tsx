"use client"

import { useQuery } from "@tanstack/react-query"
import { coursesApi } from "@/lib/api"
import type { ActivityType, StudentGradeSuggestionDto } from "@/types"
import { activityTypeIcon, Spinner } from "./shared"

export function CourseGrades({ sectionId, userRole }: { sectionId: string; userRole: string }) {
  const { data: activities = [], isLoading } = useQuery({
    queryKey: ["course", sectionId, "activities"],
    queryFn: () => coursesApi.getActivities(sectionId),
  })

  const { data: mySubmissions = [], isLoading: loadingSubs } = useQuery({
    queryKey: ["course", sectionId, "my-submissions"],
    queryFn: () => coursesApi.getMySubmissions(sectionId),
    enabled: userRole === "Estudiante",
  })

  const { data: mySuggestion, isLoading: loadingMySuggestion } = useQuery({
    queryKey: ["course", sectionId, "my-grade-suggestion"],
    queryFn: () => coursesApi.getMyGradeSuggestion(sectionId),
    enabled: userRole === "Estudiante",
  })

  const { data: suggestions = [], isLoading: loadingSuggestions } = useQuery({
    queryKey: ["course", sectionId, "grade-suggestions"],
    queryFn: () => coursesApi.getGradeSuggestions(sectionId),
    enabled: userRole === "Profesor" || userRole === "Admin",
  })

  if (isLoading || loadingSubs || loadingMySuggestion || loadingSuggestions) return <Spinner />

  const totalWeight = activities.reduce((sum, a) => sum + a.weight, 0)

  return (
    <div className="space-y-4">
      <h2 className="text-lg font-bold text-foreground">Calificaciones</h2>

      {/* ── Vista Estudiante ── */}
      {userRole === "Estudiante" && mySuggestion && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div className="bg-card border border-border rounded-2xl p-5">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1">
              Promedio de actividades
            </p>
            {mySuggestion.suggestedScore !== null ? (
              <p className={`text-3xl font-bold ${mySuggestion.suggestedScore >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"}`}>
                {mySuggestion.suggestedScore.toFixed(1)}
              </p>
            ) : (
              <p className="text-2xl font-bold text-muted-foreground">—</p>
            )}
            <p className="text-xs text-muted-foreground mt-1">
              {mySuggestion.completedActivities}/{mySuggestion.totalActivities} actividades calificadas
            </p>
          </div>

          <div className="bg-card border border-primary/30 rounded-2xl p-5">
            <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-1">
              Calificación oficial
            </p>
            {mySuggestion.officialGrade !== null ? (
              <p className={`text-3xl font-bold ${mySuggestion.officialGrade >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"}`}>
                {mySuggestion.officialGrade.toFixed(1)}
              </p>
            ) : (
              <p className="text-2xl font-bold text-muted-foreground">Pendiente</p>
            )}
            <p className="text-xs text-muted-foreground mt-1">Asignada por el profesor · Esta es la nota que cuenta</p>
          </div>
        </div>
      )}

      {/* ── Vista Profesor ── */}
      {(userRole === "Profesor" || userRole === "Admin") && (
        <>
          <div className="bg-amber-500/5 border border-amber-500/20 rounded-2xl px-5 py-3.5 flex items-start gap-3">
            <span className="text-amber-500 text-base leading-none mt-0.5">ℹ</span>
            <p className="text-xs text-amber-700 dark:text-amber-400 leading-relaxed">
              <strong>Nota sugerida</strong> = cálculo automático a partir de actividades ponderadas (0–10). Es solo una referencia.
              La <strong>nota oficial</strong> es la que aparece en el expediente del estudiante y se asigna manualmente desde
              la sección <strong>Calificaciones</strong>.
            </p>
          </div>

          {suggestions.length > 0 && (
            <div className="bg-card border border-border rounded-2xl overflow-hidden">
              <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-3 px-5 py-3 border-b border-border text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                <span>Estudiante</span>
                <span className="text-center">Actividades</span>
                <span className="text-center">Nota sugerida</span>
                <span className="text-center">Nota oficial</span>
                <span className="text-center">Estado</span>
              </div>
              <div className="divide-y divide-border">
                {suggestions.map((s) => (
                  <SuggestionRow key={s.studentId} s={s} />
                ))}
              </div>
            </div>
          )}

          {suggestions.length === 0 && (
            <p className="text-xs text-muted-foreground text-center py-8">Sin alumnos inscritos</p>
          )}
        </>
      )}

      {/* ── Tabla de actividades ── */}
      <div className="bg-card border border-border rounded-2xl overflow-hidden">
        <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-3 px-5 py-3 border-b border-border text-xs font-semibold text-muted-foreground uppercase tracking-wide">
          <span>Actividad</span>
          <span className="text-center">Tipo</span>
          <span className="text-center">Ponderación</span>
          <span className="text-center">Máx.</span>
          {userRole === "Estudiante" && <span className="text-center">Tu nota</span>}
        </div>
        <div className="divide-y divide-border">
          {activities.map((act) => {
            const sub = mySubmissions.find((s) => s.activityId === act.id)
            const Icon = activityTypeIcon(act.type as ActivityType)
            return (
              <div key={act.id} className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-3 items-center px-5 py-3.5">
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 shrink-0">
                    <Icon className="h-4 w-4 text-primary" />
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-foreground truncate">{act.title}</p>
                    {act.dueDate && (
                      <p className="text-xs text-muted-foreground">Vence: {new Date(act.dueDate).toLocaleDateString("es-DO")}</p>
                    )}
                  </div>
                </div>
                <span className="text-xs text-center text-muted-foreground">{act.type}</span>
                <span className="text-sm text-center font-medium text-foreground">{act.weight}%</span>
                <span className="text-sm text-center text-muted-foreground">{act.maxScore}</span>
                {userRole === "Estudiante" && (
                  <div className="text-center">
                    {sub?.score !== undefined && sub.score !== null ? (
                      <span className={`text-sm font-bold ${sub.score >= act.maxScore * 0.7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"}`}>
                        {sub.score}/{act.maxScore}
                      </span>
                    ) : (
                      <span className="text-xs text-muted-foreground">{sub ? "Pendiente calif." : "Sin entregar"}</span>
                    )}
                  </div>
                )}
              </div>
            )
          })}
          {activities.length === 0 && (
            <p className="text-xs text-muted-foreground text-center py-8">Sin actividades configuradas</p>
          )}
        </div>
        {activities.length > 0 && (
          <div className="flex items-center justify-end gap-4 px-5 py-3 border-t border-border bg-muted/30">
            <span className="text-xs font-semibold text-muted-foreground uppercase">Total ponderación</span>
            <span className="text-sm font-bold text-foreground">{totalWeight}%</span>
          </div>
        )}
      </div>
    </div>
  )
}

function SuggestionRow({ s }: { s: StudentGradeSuggestionDto }) {
  const allDone = s.completedActivities === s.totalActivities && s.totalActivities > 0
  const partial = s.completedActivities > 0 && s.completedActivities < s.totalActivities

  return (
    <div className="grid grid-cols-[1fr_auto_auto_auto_auto] gap-3 items-center px-5 py-3.5">
      <div className="min-w-0">
        <p className="text-sm font-medium text-foreground">{s.studentName}</p>
        <p className="text-xs text-muted-foreground">{s.studentCode}</p>
      </div>
      <span className="text-xs text-center text-muted-foreground tabular-nums">
        {s.completedActivities}/{s.totalActivities}
      </span>
      <span className={`text-sm text-center font-bold tabular-nums ${
        s.suggestedScore === null ? "text-muted-foreground" :
        s.suggestedScore >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"
      }`}>
        {s.suggestedScore !== null ? s.suggestedScore.toFixed(1) : "—"}
      </span>
      <span className={`text-sm text-center font-bold tabular-nums ${
        s.officialGrade === null ? "text-muted-foreground" :
        s.officialGrade >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"
      }`}>
        {s.officialGrade !== null ? s.officialGrade.toFixed(1) : "—"}
      </span>
      <span className={`text-xs text-center px-2 py-0.5 rounded-full font-medium ${
        s.officialGrade !== null
          ? "bg-green-500/10 text-green-700 dark:text-green-400"
          : allDone
          ? "bg-primary/10 text-primary"
          : partial
          ? "bg-amber-500/10 text-amber-700 dark:text-amber-400"
          : "bg-muted text-muted-foreground"
      }`}>
        {s.officialGrade !== null ? "Oficial" : allDone ? "Completa" : partial ? "Parcial" : "Sin entregar"}
      </span>
    </div>
  )
}

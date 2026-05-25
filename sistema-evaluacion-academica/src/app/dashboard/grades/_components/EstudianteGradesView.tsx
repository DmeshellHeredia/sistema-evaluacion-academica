"use client"

import { useState } from "react"
import { useQuery } from "@tanstack/react-query"
import { Loader2, ChevronLeft, ChevronRight } from "lucide-react"
import { studentsApi, gradesApi } from "@/lib/api"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"

export function EstudianteGradesView() {
  const [page, setPage] = useState(1)

  const { data: studentData, isLoading: loadingStudent, isError: studentError, error: studentErr, refetch: refetchStudent } = useQuery({
    queryKey: ["student", "me"],
    queryFn: studentsApi.me,
  })

  const {
    data: gradesData,
    isLoading: loadingGrades,
    isError,
    error,
    refetch,
  } = useQuery({
    queryKey: ["grades", "student", studentData?.id, page],
    queryFn: () => gradesApi.getByStudent(studentData!.id, page, 20),
    enabled: !!studentData?.id,
  })

  const grades = gradesData?.items ?? []
  const totalPages = gradesData?.totalPages ?? 1
  const totalCount = gradesData?.totalCount ?? 0

  const isLoading = loadingStudent || loadingGrades

  const average = studentData?.overallAverage ?? null
  const averageCategory = studentData?.averageCategory ?? null

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    )
  }

  if (studentError) {
    return <ErrorState error={studentErr} onRetry={() => void refetchStudent()} />
  }

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  return (
    <div className="space-y-5">
      {average !== null && averageCategory !== null && (
        <div className="bg-card border border-border rounded-2xl p-5 shadow-sm flex items-center justify-between">
          <div>
            <p className="text-sm font-semibold text-foreground">Promedio general</p>
            <p className="text-xs text-muted-foreground mt-0.5">{totalCount} calificación{totalCount !== 1 ? "es" : ""} registrada{totalCount !== 1 ? "s" : ""}</p>
          </div>
          <GradeBadge category={averageCategory} value={average} />
        </div>
      )}

      <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
        <div className="px-5 py-3.5 border-b border-border bg-muted/20">
          <p className="text-sm font-semibold text-foreground">Mis Calificaciones</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Materia</th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">Calificación</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">Período</th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden lg:table-cell">Comentarios</th>
              </tr>
            </thead>
            <tbody>
              {grades.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-4 py-12 text-center text-sm text-muted-foreground">
                    No tienes calificaciones registradas aún
                  </td>
                </tr>
              ) : (
                grades.map((grade) => (
                  <tr key={grade.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3">
                      <p className="font-medium text-foreground">{grade.subjectName}</p>
                      <p className="text-xs text-muted-foreground">{grade.subjectCode}</p>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <GradeBadge category={grade.category} value={grade.value} size="sm" />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground hidden sm:table-cell">{grade.period}</td>
                    <td className="px-4 py-3 text-muted-foreground hidden lg:table-cell max-w-50 truncate">
                      {grade.comments ?? <span className="opacity-40">—</span>}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-border bg-muted/20">
            <p className="text-xs text-muted-foreground">
              Página {page} de {totalPages} — {totalCount} calificaciones
            </p>
            <div className="flex items-center gap-1">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                aria-label="Página anterior"
                className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronLeft className="h-4 w-4" />
              </button>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
                aria-label="Página siguiente"
                className="flex h-7 w-7 items-center justify-center rounded-lg border border-border hover:bg-muted disabled:opacity-40 disabled:cursor-not-allowed transition-colors cursor-pointer"
              >
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

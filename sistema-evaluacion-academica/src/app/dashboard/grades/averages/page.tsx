"use client"

import { useState } from "react"
import { useQuery } from "@tanstack/react-query"
import { TrendingUp, Users, Award, AlertCircle, Search } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { studentsApi } from "@/lib/api"
import { GradeBadge } from "@/components/grade-badge"
import { StatsCard } from "@/components/stats-card"
import { ErrorState } from "@/components/error-state"

export default function AveragesPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState("")
  const [categoryFilter, setCategoryFilter] = useState<string>("all")

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ["students", 1, 100],
    queryFn: () => studentsApi.getAll(1, 100),
    enabled: user?.role === "Admin",
  })

  const allStudents = data?.items ?? []
  const withGrades = allStudents.filter((s) => s.overallAverage !== null)

  // Stats
  const excellent = withGrades.filter((s) => s.averageCategory === "Excelente").length
  const good = withGrades.filter((s) => s.averageCategory === "Buena").length
  const needsWork = withGrades.filter((s) => s.averageCategory === "Por mejorar").length
  const overallAvg =
    withGrades.length > 0
      ? withGrades.reduce((sum, s) => sum + s.overallAverage!, 0) / withGrades.length
      : 0

  // Filtered list
  const filtered = withGrades
    .filter((s) => {
      const matchSearch =
        !search ||
        s.fullName.toLowerCase().includes(search.toLowerCase()) ||
        s.studentCode.toLowerCase().includes(search.toLowerCase()) ||
        s.career.toLowerCase().includes(search.toLowerCase())
      const matchCategory =
        categoryFilter === "all" || s.averageCategory === categoryFilter
      return matchSearch && matchCategory
    })
    .sort((a, b) => b.overallAverage! - a.overallAverage!)

  if (isError) {
    return <ErrorState error={error} onRetry={() => void refetch()} />
  }

  if (user?.role !== "Admin") {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-center space-y-4">
        <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10">
          <Award className="h-8 w-8 text-primary" />
        </div>
        <div>
          <h2 className="text-lg font-semibold text-foreground">Acceso restringido</h2>
          <p className="text-sm text-muted-foreground mt-1 max-w-sm">
            Esta sección es solo para administradores.
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <StatsCard
          title="Promedio general"
          value={overallAvg > 0 ? overallAvg.toFixed(2) : "—"}
          subtitle="De estudiantes calificados"
          icon={TrendingUp}
          color="indigo"
        />
        <StatsCard
          title="Calificados"
          value={withGrades.length}
          subtitle={`de ${allStudents.length} estudiantes`}
          icon={Users}
          color="blue"
        />
        <StatsCard
          title="Excelente"
          value={excellent}
          subtitle="Promedio ≥ 9.0"
          icon={Award}
          color="emerald"
        />
        <StatsCard
          title="Debe mejorar"
          value={needsWork}
          subtitle="Promedio < 7.0"
          icon={AlertCircle}
          color="violet"
        />
      </div>

      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar estudiante o carrera..."
            className="w-full h-9 pl-9 pr-3 rounded-lg border border-input bg-background text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring/50 focus:border-ring transition-colors"
          />
        </div>
        <div className="flex gap-2">
          {[
            { key: "all", label: "Todos" },
            { key: "Excelente", label: "Excelente" },
            { key: "Buena", label: "Buena" },
            { key: "Por mejorar", label: "Por mejorar" },
          ].map((f) => (
            <button
              key={f.key}
              onClick={() => setCategoryFilter(f.key)}
              className={`h-9 px-3 rounded-lg text-sm font-medium transition-colors cursor-pointer border ${
                categoryFilter === f.key
                  ? "bg-primary text-primary-foreground border-primary"
                  : "bg-background text-muted-foreground border-input hover:bg-muted"
              }`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-card border border-border rounded-2xl shadow-sm overflow-hidden">
        <div className="px-5 py-3.5 border-b border-border bg-muted/20 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-foreground">Ranking de promedios</h3>
          <span className="text-xs text-muted-foreground">{filtered.length} resultados</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-border bg-muted/40">
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide w-12">
                  #
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Estudiante
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden md:table-cell">
                  Carrera
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide hidden sm:table-cell">
                  Sem.
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Promedio
                </th>
                <th className="text-right px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
                  Barra
                </th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 5 }).map((_, i) => (
                  <tr key={i} className="border-b border-border/50">
                    {Array.from({ length: 6 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <div className="h-4 bg-muted animate-pulse rounded" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-14 text-center text-muted-foreground text-sm">
                    {allStudents.length === 0
                      ? "No hay estudiantes registrados"
                      : withGrades.length === 0
                      ? "Ningún estudiante tiene calificaciones aún"
                      : "No se encontraron resultados"}
                  </td>
                </tr>
              ) : (
                filtered.map((student, idx) => {
                  const pct = (student.overallAverage! / 10) * 100
                  const barColor =
                    student.averageCategory === "Excelente"
                      ? "bg-emerald-500"
                      : student.averageCategory === "Buena"
                      ? "bg-blue-500"
                      : "bg-red-400"

                  return (
                    <tr
                      key={student.id}
                      className="border-b border-border/50 hover:bg-muted/30 transition-colors"
                    >
                      <td className="px-4 py-3 text-center">
                        {idx < 3 ? (
                          <div
                            className={`inline-flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold ${
                              idx === 0
                                ? "bg-amber-100 text-amber-700"
                                : idx === 1
                                ? "bg-slate-100 text-slate-600"
                                : "bg-orange-100 text-orange-700"
                            }`}
                          >
                            {idx + 1}
                          </div>
                        ) : (
                          <span className="text-xs text-muted-foreground">{idx + 1}</span>
                        )}
                      </td>

                      <td className="px-4 py-3">
                        <p className="font-medium text-foreground">{student.fullName}</p>
                        <p className="text-xs text-muted-foreground font-mono">
                          {student.studentCode}
                        </p>
                      </td>

                      <td className="px-4 py-3 text-muted-foreground hidden md:table-cell">
                        {student.career}
                      </td>

                      <td className="px-4 py-3 text-center text-muted-foreground hidden sm:table-cell">
                        {student.semester}
                      </td>

                      <td className="px-4 py-3 text-center">
                        <GradeBadge
                          category={student.averageCategory}
                          value={student.overallAverage!}
                          size="sm"
                        />
                      </td>

                      <td className="px-4 py-3">
                        <div className="flex items-center gap-2 min-w-20">
                          <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                            <div
                              className={`h-full rounded-full ${barColor} transition-all duration-500`}
                              style={{ width: `${pct}%` }}
                            />
                          </div>
                          <span className="text-xs font-mono text-muted-foreground w-8 text-right shrink-0">
                            {pct.toFixed(0)}%
                          </span>
                        </div>
                      </td>
                    </tr>
                  )
                })
              )}
            </tbody>
          </table>
        </div>

        {withGrades.length > 0 && !isLoading && (
          <div className="px-5 py-4 border-t border-border bg-muted/10 grid grid-cols-3 gap-4 text-center">
            {[
              { label: "Excelente", count: excellent, color: "text-emerald-600" },
              { label: "Buena", count: good, color: "text-blue-600" },
              { label: "Debe mejorar", count: needsWork, color: "text-red-500" },
            ].map((cat) => (
              <div key={cat.label}>
                <p className={`text-lg font-bold ${cat.color}`}>{cat.count}</p>
                <p className="text-xs text-muted-foreground">{cat.label}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

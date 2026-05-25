"use client"

import { useQuery } from "@tanstack/react-query"
import { GraduationCap, User, Mail, BookOpen, BarChart3, Star, CheckCircle, XCircle, Loader2 } from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { studentsApi, gradesApi } from "@/lib/api"
import { GradeBadge } from "@/components/grade-badge"
import { ErrorState } from "@/components/error-state"
import { gradeCategory } from "@/lib/grades"

export default function ProfilePage() {
  const { user } = useAuth()

  if (user?.role === "Estudiante") return <EstudianteProfile />
  return <GenericProfile />
}

function GenericProfile() {
  const { user } = useAuth()

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <h2 className="text-xl font-bold text-foreground">Mi Perfil</h2>

      <div className="bg-card border border-border rounded-2xl p-6 shadow-sm">
        <div className="flex items-center gap-5">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10">
            <User className="h-8 w-8 text-primary" />
          </div>
          <div>
            <h3 className="text-lg font-bold text-foreground">{user?.fullName}</h3>
            <p className="text-sm text-muted-foreground">{user?.role}</p>
          </div>
        </div>

        <div className="mt-6 space-y-4 border-t border-border pt-5">
          <InfoRow icon={Mail} label="Correo electrónico" value={user?.email ?? "—"} />
          <InfoRow icon={User} label="Rol" value={user?.role ?? "—"} />
        </div>
      </div>
    </div>
  )
}

function EstudianteProfile() {
  const { user } = useAuth()

  const { data: studentData, isLoading: loadingStudent, isError, error, refetch } = useQuery({
    queryKey: ["student", "me"],
    queryFn: studentsApi.me,
  })

  const { data: gradesData, isLoading: loadingGrades } = useQuery({
    queryKey: ["grades", "student", user?.id, "profile"],
    queryFn: () => gradesApi.getByStudent(user!.id, 1, 100),
    enabled: !!user?.id,
  })
  const grades = gradesData?.items ?? []

  if (loadingStudent) return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div>
  if (isError) return <ErrorState error={error} onRetry={() => void refetch()} />

  const approved = grades.filter((g) => g.value >= 7)
  const failed = grades.filter((g) => g.value < 7)
  const average = grades.length > 0 ? grades.reduce((sum, g) => sum + g.value, 0) / grades.length : null

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <h2 className="text-xl font-bold text-foreground">Mi Perfil</h2>

      <div className="bg-card border border-border rounded-2xl p-6 shadow-sm">
        <div className="flex items-start gap-5 flex-wrap">
          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-primary/10 shrink-0">
            <GraduationCap className="h-8 w-8 text-primary" />
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-lg font-bold text-foreground">{studentData?.fullName}</h3>
            <p className="text-sm text-muted-foreground">{studentData?.career} · Semestre {studentData?.semester}</p>
            <p className="text-xs text-muted-foreground font-mono mt-0.5">{studentData?.studentCode}</p>
          </div>
          {average !== null && (
            <GradeBadge category={gradeCategory(average)} value={average} />
          )}
        </div>

        <div className="mt-6 space-y-4 border-t border-border pt-5">
          <InfoRow icon={Mail} label="Correo electrónico" value={studentData?.email ?? "—"} />
          <InfoRow icon={BookOpen} label="Carrera" value={studentData?.career ?? "—"} />
          <InfoRow icon={GraduationCap} label="Semestre" value={`Semestre ${studentData?.semester}`} />
          <InfoRow icon={User} label="Código" value={studentData?.studentCode ?? "—"} />
        </div>
      </div>

      {!loadingGrades && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            icon={BarChart3}
            label={average !== null ? "Promedio General" : "Sin calificaciones"}
            value={average !== null ? average.toFixed(2) : "—"}
            color="indigo"
          />
          <StatCard
            icon={CheckCircle}
            label="Materias Aprobadas"
            value={approved.length.toString()}
            color="emerald"
          />
          <StatCard
            icon={XCircle}
            label="Materias Reprobadas"
            value={failed.length.toString()}
            color={failed.length > 0 ? "red" : "blue"}
          />
        </div>
      )}

      {!loadingGrades && grades.length > 0 && (
        <div className="bg-card border border-border rounded-2xl overflow-hidden">
          <div className="px-5 py-4 border-b border-border flex items-center gap-2">
            <Star className="h-4 w-4 text-primary" />
            <h3 className="text-sm font-semibold text-foreground">Historial de Calificaciones</h3>
          </div>
          <div className="divide-y divide-border">
            {grades.map((g) => (
              <div key={g.id} className="flex items-center justify-between px-5 py-3.5">
                <div>
                  <p className="text-sm font-medium text-foreground">{g.subjectName}</p>
                  <p className="text-xs text-muted-foreground">{g.subjectCode} · {g.period}</p>
                  {g.comments && <p className="text-xs text-muted-foreground/70 mt-0.5 italic">{g.comments}</p>}
                </div>
                <div className="text-right">
                  <p className={`text-lg font-bold ${g.value >= 7 ? "text-emerald-600 dark:text-emerald-400" : "text-red-500"}`}>{g.value}</p>
                  <p className="text-xs text-muted-foreground">{g.category}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function InfoRow({ icon: Icon, label, value }: { icon: React.ElementType; label: string; value: string }) {
  return (
    <div className="flex items-center gap-3">
      <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-muted shrink-0">
        <Icon className="h-4 w-4 text-muted-foreground" />
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-sm font-medium text-foreground">{value}</p>
      </div>
    </div>
  )
}

const colorMap: Record<string, string> = {
  indigo: "bg-indigo-500/10 text-indigo-600 dark:text-indigo-400",
  emerald: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  red: "bg-red-500/10 text-red-600 dark:text-red-400",
  blue: "bg-blue-500/10 text-blue-600 dark:text-blue-400",
}

function StatCard({ icon: Icon, label, value, color }: { icon: React.ElementType; label: string; value: string; color: string }) {
  return (
    <div className="bg-card border border-border rounded-2xl p-5 flex items-center gap-4">
      <div className={`flex h-11 w-11 items-center justify-center rounded-xl shrink-0 ${colorMap[color] ?? "bg-primary/10 text-primary"}`}>
        <Icon className="h-5 w-5" />
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-2xl font-bold text-foreground">{value}</p>
      </div>
    </div>
  )
}

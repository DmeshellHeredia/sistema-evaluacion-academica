"use client"

import { useQuery } from "@tanstack/react-query"
import { ClipboardList, Megaphone } from "lucide-react"
import { coursesApi } from "@/lib/api"
import type { ActivityType, CourseOverviewDto } from "@/types"
import { DAY_LABELS } from "@/types"
import { activityTypeIcon } from "./shared"

export function CourseHome({ overview, sectionId }: { overview: CourseOverviewDto; sectionId: string }) {
  const { data: announcements = [] } = useQuery({
    queryKey: ["course", sectionId, "announcements"],
    queryFn: () => coursesApi.getAnnouncements(sectionId),
  })

  const { data: activities = [] } = useQuery({
    queryKey: ["course", sectionId, "activities"],
    queryFn: () => coursesApi.getActivities(sectionId),
  })

  const upcoming = activities
    .filter((a) => a.dueDate && new Date(a.dueDate) > new Date())
    .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
    .slice(0, 3)

  const latestAnnouncements = announcements.slice(0, 3)

  return (
    <div className="space-y-6">
      <div className="rounded-2xl bg-linear-to-r from-primary to-primary/70 p-6 text-primary-foreground shadow-lg shadow-primary/20">
        <h2 className="text-xl font-bold">{overview.subjectName}</h2>
        <p className="text-primary-foreground/80 text-sm mt-1">
          Sección {overview.sectionCode} · {DAY_LABELS[overview.dayOfWeek]} {overview.startTime}–{overview.endTime} · {overview.modality}
        </p>
        <div className="flex items-center gap-4 mt-4 text-sm text-primary-foreground/90">
          <span>{overview.enrolledCount} estudiantes inscritos</span>
          <span>·</span>
          <span>{activities.length} actividades</span>
          <span>·</span>
          <span>{announcements.length} anuncios</span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-card border border-border rounded-2xl overflow-hidden">
          <div className="px-5 py-4 border-b border-border">
            <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
              <ClipboardList className="h-4 w-4 text-primary" />
              Próximas actividades
            </h3>
          </div>
          <div className="divide-y divide-border">
            {upcoming.length === 0 ? (
              <p className="text-xs text-muted-foreground text-center py-6">Sin actividades próximas</p>
            ) : upcoming.map((a) => {
              const Icon = activityTypeIcon(a.type as ActivityType)
              const due = new Date(a.dueDate!)
              const isOverdue = due < new Date()
              return (
                <div key={a.id} className="flex items-start gap-3 px-5 py-3.5">
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary/10 shrink-0">
                    <Icon className="h-4 w-4 text-primary" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-foreground truncate">{a.title}</p>
                    <p className={`text-xs mt-0.5 ${isOverdue ? "text-red-500" : "text-muted-foreground"}`}>
                      {isOverdue ? "Vencida: " : "Vence: "}{due.toLocaleDateString("es-DO", { day: "numeric", month: "short" })}
                    </p>
                  </div>
                  <span className="text-xs text-muted-foreground shrink-0">{a.maxScore} pts</span>
                </div>
              )
            })}
          </div>
        </div>

        <div className="bg-card border border-border rounded-2xl overflow-hidden">
          <div className="px-5 py-4 border-b border-border">
            <h3 className="text-sm font-semibold text-foreground flex items-center gap-2">
              <Megaphone className="h-4 w-4 text-primary" />
              Últimos anuncios
            </h3>
          </div>
          <div className="divide-y divide-border">
            {latestAnnouncements.length === 0 ? (
              <p className="text-xs text-muted-foreground text-center py-6">Sin anuncios recientes</p>
            ) : latestAnnouncements.map((ann) => (
              <div key={ann.id} className="px-5 py-3.5">
                <p className="text-sm font-medium text-foreground">{ann.title}</p>
                <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{ann.content}</p>
                <p className="text-xs text-muted-foreground/60 mt-1">
                  {new Date(ann.createdAt).toLocaleDateString("es-DO")} · {ann.authorName}
                </p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}

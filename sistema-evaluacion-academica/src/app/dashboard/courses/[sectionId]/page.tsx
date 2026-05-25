"use client"

import { useState } from "react"
import { useParams } from "next/navigation"
import { useQuery } from "@tanstack/react-query"
import {
  BookOpen, Users, Star, ClipboardList, BookMarked, Home,
  Loader2, ChevronRight, Clock, Megaphone, GraduationCap,
} from "lucide-react"
import { useAuth } from "@/context/auth-context"
import { coursesApi } from "@/lib/api"
import { DAY_LABELS } from "@/types"
import { CourseHome } from "./_components/CourseHome"
import { CourseParticipants } from "./_components/CourseParticipants"
import { CourseGrades } from "./_components/CourseGrades"
import { CourseActivities } from "./_components/CourseActivities"
import { CourseResources } from "./_components/CourseResources"
import { CourseAnnouncements } from "./_components/CourseAnnouncements"

type Tab = "inicio" | "participantes" | "calificaciones" | "actividades" | "recursos" | "anuncios"

export default function CoursePage() {
  const { sectionId } = useParams<{ sectionId: string }>()
  const { user } = useAuth()
  const [activeTab, setActiveTab] = useState<Tab>("inicio")

  const { data: overview, isLoading } = useQuery({
    queryKey: ["course", sectionId, "overview"],
    queryFn: () => coursesApi.getOverview(sectionId),
  })

  if (isLoading) {
    return <div className="flex justify-center py-20"><Loader2 className="h-8 w-8 animate-spin text-primary" /></div>
  }

  if (!overview) {
    return (
      <div className="bg-card border border-border rounded-2xl p-12 text-center">
        <p className="text-sm text-muted-foreground">Sección no encontrada o sin acceso.</p>
      </div>
    )
  }

  const tabs: { id: Tab; label: string; icon: React.ElementType }[] = [
    { id: "inicio", label: "Inicio", icon: Home },
    { id: "participantes", label: "Participantes", icon: Users },
    { id: "calificaciones", label: "Calificaciones", icon: Star },
    { id: "actividades", label: "Actividades", icon: ClipboardList },
    { id: "recursos", label: "Recursos", icon: BookMarked },
    { id: "anuncios", label: "Anuncios", icon: Megaphone },
  ]

  const userRole = user?.role ?? ""

  return (
    <div className="flex flex-col lg:flex-row gap-6 min-h-[calc(100vh-8rem)]">
      <aside className="lg:w-64 shrink-0">
        <div className="bg-card border border-border rounded-2xl overflow-hidden sticky top-4">
          <div className="bg-primary/10 px-4 py-5 border-b border-border">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/20 mb-3">
              <BookOpen className="h-5 w-5 text-primary" />
            </div>
            <p className="font-bold text-foreground text-sm leading-snug">{overview.subjectName}</p>
            <p className="text-xs text-muted-foreground mt-0.5">{overview.subjectCode} · Sec. {overview.sectionCode}</p>
          </div>

          <div className="px-4 py-3 border-b border-border space-y-2">
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <GraduationCap className="h-3.5 w-3.5 shrink-0 text-primary" />
              <span>{overview.professorName}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <Clock className="h-3.5 w-3.5 shrink-0 text-primary" />
              <span>{DAY_LABELS[overview.dayOfWeek]} {overview.startTime}–{overview.endTime}</span>
            </div>
            <div className="flex items-center gap-2 text-xs text-muted-foreground">
              <Users className="h-3.5 w-3.5 shrink-0 text-primary" />
              <span>{overview.enrolledCount}/{overview.capacity} estudiantes · {overview.modality}</span>
            </div>
          </div>

          <nav className="p-2">
            {tabs.map(({ id, label, icon: Icon }) => (
              <button
                key={id}
                onClick={() => setActiveTab(id)}
                className={`flex w-full items-center gap-2.5 px-3 py-2.5 rounded-xl text-sm font-medium transition-all cursor-pointer ${
                  activeTab === id
                    ? "bg-primary text-primary-foreground shadow-sm"
                    : "text-muted-foreground hover:bg-muted hover:text-foreground"
                }`}
              >
                <Icon className="h-4 w-4 shrink-0" />
                {label}
                {activeTab === id && <ChevronRight className="h-3.5 w-3.5 ml-auto" />}
              </button>
            ))}
          </nav>
        </div>
      </aside>

      <main className="flex-1 min-w-0">
        {activeTab === "inicio" && <CourseHome overview={overview} sectionId={sectionId} />}
        {activeTab === "participantes" && <CourseParticipants sectionId={sectionId} />}
        {activeTab === "calificaciones" && <CourseGrades sectionId={sectionId} userRole={userRole} />}
        {activeTab === "actividades" && <CourseActivities sectionId={sectionId} userRole={userRole} />}
        {activeTab === "recursos" && <CourseResources sectionId={sectionId} userRole={userRole} />}
        {activeTab === "anuncios" && <CourseAnnouncements sectionId={sectionId} userRole={userRole} />}
      </main>
    </div>
  )
}

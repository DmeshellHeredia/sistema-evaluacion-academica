"use client"

import { useState } from "react"
import { useQuery, useQueryClient } from "@tanstack/react-query"
import { Plus, Download, BookMarked } from "lucide-react"
import { coursesApi } from "@/lib/api"
import type { ActivityType } from "@/types"
import { Spinner } from "./shared"
import { CourseActivityModal } from "./CourseActivities"

export function CourseResources({ sectionId, userRole }: { sectionId: string; userRole: string }) {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)

  const { data: activities = [], isLoading } = useQuery({
    queryKey: ["course", sectionId, "activities"],
    queryFn: () => coursesApi.getActivities(sectionId),
  })

  const resources = activities.filter((a) => a.type === "Recurso")

  if (isLoading) return <Spinner />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-foreground">Recursos</h2>
        {userRole === "Profesor" && (
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors cursor-pointer"
          >
            <Plus className="h-4 w-4" />
            Agregar recurso
          </button>
        )}
      </div>

      {resources.length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-12 text-center">
          <BookMarked className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm font-semibold text-foreground">Sin recursos</p>
          {userRole === "Profesor" && <p className="text-xs text-muted-foreground mt-1">Agrega materiales de estudio para los estudiantes.</p>}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {resources.map((r) => (
            <div key={r.id} className="flex items-start gap-3 p-4 bg-card border border-border rounded-2xl hover:border-primary/30 transition-colors">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 shrink-0">
                <BookMarked className="h-5 w-5 text-primary" />
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-foreground">{r.title}</p>
                <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{r.description}</p>
                {r.resourceUrl && (
                  <a
                    href={r.resourceUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1 mt-2 text-xs text-primary hover:underline"
                  >
                    <Download className="h-3 w-3" />
                    Abrir recurso
                  </a>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {showCreate && (
        <CourseActivityModal
          sectionId={sectionId}
          defaultType={"Recurso" as ActivityType}
          onClose={() => setShowCreate(false)}
          onSuccess={() => { setShowCreate(false); qc.invalidateQueries({ queryKey: ["course", sectionId, "activities"] }) }}
        />
      )}
    </div>
  )
}

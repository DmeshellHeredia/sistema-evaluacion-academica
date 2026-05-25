"use client"

import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Megaphone, Plus, Trash2 } from "lucide-react"
import { coursesApi, getErrorMessage } from "@/lib/api"
import type { CreateAnnouncementDto } from "@/types"
import { Spinner, ModalOverlay, Field, DeleteConfirmModal, inputCls, primaryBtnCls, cancelBtnCls } from "./shared"

export function CourseAnnouncements({ sectionId, userRole }: { sectionId: string; userRole: string }) {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const { data: announcements = [], isLoading } = useQuery({
    queryKey: ["course", sectionId, "announcements"],
    queryFn: () => coursesApi.getAnnouncements(sectionId),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => coursesApi.deleteAnnouncement(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["course", sectionId, "announcements"] })
      setDeleteConfirmId(null)
    },
  })

  if (isLoading) return <Spinner />

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-foreground">Anuncios</h2>
        {userRole === "Profesor" && (
          <button
            onClick={() => setShowCreate(true)}
            className="flex items-center gap-2 px-4 py-2 rounded-xl bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors cursor-pointer"
          >
            <Plus className="h-4 w-4" />
            Nuevo anuncio
          </button>
        )}
      </div>

      {announcements.length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-12 text-center">
          <Megaphone className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm font-semibold text-foreground">Sin anuncios</p>
          {userRole === "Profesor" && <p className="text-xs text-muted-foreground mt-1">Publica anuncios para informar a tus estudiantes.</p>}
        </div>
      ) : (
        <div className="space-y-3">
          {announcements.map((ann) => (
            <div key={ann.id} className="bg-card border border-border rounded-2xl p-5">
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h3 className="text-sm font-bold text-foreground">{ann.title}</h3>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {ann.authorName} · {new Date(ann.createdAt).toLocaleDateString("es-DO", { year: "numeric", month: "long", day: "numeric" })}
                  </p>
                </div>
                {userRole === "Profesor" && (
                  <button
                    onClick={() => setDeleteConfirmId(ann.id)}
                    className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer shrink-0"
                    title="Eliminar anuncio"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
              <p className="text-sm text-foreground/80 mt-3 whitespace-pre-wrap">{ann.content}</p>
            </div>
          ))}
        </div>
      )}

      {showCreate && (
        <AnnouncementModal
          sectionId={sectionId}
          onClose={() => setShowCreate(false)}
          onSuccess={() => { setShowCreate(false); qc.invalidateQueries({ queryKey: ["course", sectionId, "announcements"] }) }}
        />
      )}

      {deleteConfirmId && (
        <DeleteConfirmModal
          title="Eliminar anuncio"
          onConfirm={() => deleteMutation.mutate(deleteConfirmId)}
          onCancel={() => setDeleteConfirmId(null)}
          loading={deleteMutation.isPending}
        />
      )}
    </div>
  )
}

function AnnouncementModal({ sectionId, onClose, onSuccess }: { sectionId: string; onClose: () => void; onSuccess: () => void }) {
  const [title, setTitle] = useState("")
  const [content, setContent] = useState("")
  const [error, setError] = useState("")

  const mutation = useMutation({
    mutationFn: (data: CreateAnnouncementDto) => coursesApi.createAnnouncement(sectionId, data),
    onSuccess,
    onError: (e) => setError(getErrorMessage(e)),
  })

  return (
    <ModalOverlay onClose={onClose}>
      <form onSubmit={(e) => { e.preventDefault(); mutation.mutate({ title, content }) }} className="space-y-4">
        <h2 className="text-lg font-bold text-foreground">Nuevo anuncio</h2>
        {error && <p className="text-xs text-red-500 bg-red-500/10 px-3 py-2 rounded-lg">{error}</p>}
        <Field label="Título">
          <input value={title} onChange={(e) => setTitle(e.target.value)} required className={inputCls} />
        </Field>
        <Field label="Contenido">
          <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={5} required className={`${inputCls} resize-none`} />
        </Field>
        <div className="flex gap-2 pt-2">
          <button type="button" onClick={onClose} className={`flex-1 ${cancelBtnCls}`}>Cancelar</button>
          <button type="submit" disabled={mutation.isPending} className={`flex-1 ${primaryBtnCls}`}>
            {mutation.isPending ? "Publicando..." : "Publicar"}
          </button>
        </div>
      </form>
    </ModalOverlay>
  )
}

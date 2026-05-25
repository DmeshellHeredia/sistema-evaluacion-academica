"use client"

import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { Plus, Trash2, Edit3, Users, Upload, Download, ClipboardList } from "lucide-react"
import { coursesApi, getErrorMessage } from "@/lib/api"
import type { ActivityDto, StudentSubmissionDto, ActivityType, CreateActivityDto } from "@/types"
import {
  ACTIVITY_TYPES, activityTypeIcon, statusColor, statusIcon,
  inputCls, primaryBtnCls, cancelBtnCls,
  Spinner, Field, ModalOverlay, DeleteConfirmModal,
} from "./shared"

export function CourseActivities({ sectionId, userRole }: { sectionId: string; userRole: string }) {
  const qc = useQueryClient()
  const [showCreate, setShowCreate] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [submittingId, setSubmittingId] = useState<string | null>(null)
  const [gradingSubmissions, setGradingSubmissions] = useState<string | null>(null)
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null)

  const { data: activities = [], isLoading } = useQuery({
    queryKey: ["course", sectionId, "activities"],
    queryFn: () => coursesApi.getActivities(sectionId),
  })

  const { data: mySubmissions = [] } = useQuery({
    queryKey: ["course", sectionId, "my-submissions"],
    queryFn: () => coursesApi.getMySubmissions(sectionId),
    enabled: userRole === "Estudiante",
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => coursesApi.deleteActivity(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["course", sectionId, "activities"] })
      setDeleteConfirmId(null)
    },
  })

  if (isLoading) return <Spinner />

  const tasks = activities.filter((a) => a.type !== "Recurso")

  const renderActivity = (act: ActivityDto) => {
    const Icon = activityTypeIcon(act.type as ActivityType)
    const mySub = mySubmissions.find((s) => s.activityId === act.id)
    const StatusIcon = mySub ? statusIcon(mySub.status) : null
    const due = act.dueDate ? new Date(act.dueDate) : null
    const isOverdue = due && due < new Date()

    return (
      <div key={act.id} className="flex items-start gap-4 px-5 py-4 hover:bg-muted/30 transition-colors">
        <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary/10 shrink-0">
          <Icon className="h-4 w-4 text-primary" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <div className="min-w-0">
              <p className="text-sm font-semibold text-foreground">{act.title}</p>
              <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{act.description}</p>
            </div>
            <div className="flex items-center gap-1.5 shrink-0">
              {userRole === "Profesor" && (
                <>
                  <button onClick={() => setGradingSubmissions(act.id)} className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-blue-500/10 text-blue-500 transition-colors cursor-pointer" title="Ver entregas">
                    <Users className="h-3.5 w-3.5" />
                  </button>
                  <button onClick={() => setEditingId(act.id)} className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-primary/10 text-muted-foreground hover:text-primary transition-colors cursor-pointer">
                    <Edit3 className="h-3.5 w-3.5" />
                  </button>
                  <button onClick={() => setDeleteConfirmId(act.id)} className="flex h-7 w-7 items-center justify-center rounded-lg hover:bg-red-500/10 text-muted-foreground hover:text-red-500 transition-colors cursor-pointer" title="Eliminar actividad">
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </>
              )}
              {userRole === "Estudiante" && act.type !== "Recurso" && (
                <button onClick={() => setSubmittingId(act.id)} className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary text-primary-foreground text-xs font-medium hover:bg-primary/90 transition-colors cursor-pointer">
                  <Upload className="h-3.5 w-3.5" />
                  {mySub ? "Ver entrega" : "Entregar"}
                </button>
              )}
              {userRole === "Estudiante" && act.type === "Recurso" && act.resourceUrl && (
                <a href={act.resourceUrl} target="_blank" rel="noopener noreferrer" className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary/10 text-primary text-xs font-medium hover:bg-primary/20 transition-colors cursor-pointer">
                  <Download className="h-3.5 w-3.5" />
                  Descargar
                </a>
              )}
            </div>
          </div>
          <div className="flex items-center gap-3 mt-2 flex-wrap">
            <span className="text-xs px-2 py-0.5 rounded-full bg-primary/10 text-primary">{act.type}</span>
            {due && <span className={`text-xs ${isOverdue ? "text-red-500" : "text-muted-foreground"}`}>{isOverdue ? "Venció" : "Vence"}: {due.toLocaleDateString("es-DO", { day: "numeric", month: "short", year: "numeric" })}</span>}
            <span className="text-xs text-muted-foreground">{act.maxScore} pts · Pond. {act.weight}%</span>
            {userRole === "Profesor" && <span className="text-xs text-muted-foreground">{act.gradedSubmissions}/{act.totalSubmissions} calificadas</span>}
            {userRole === "Estudiante" && mySub && StatusIcon && (
              <span className={`flex items-center gap-1 text-xs font-medium ${statusColor(mySub.status)}`}>
                <StatusIcon className="h-3.5 w-3.5" />
                {mySub.status}
                {mySub.score !== null && ` · ${mySub.score}/${act.maxScore}`}
              </span>
            )}
          </div>
          {userRole === "Estudiante" && mySub?.feedback && (
            <div className="mt-2 p-2.5 rounded-lg bg-emerald-500/5 border border-emerald-500/20">
              <p className="text-xs text-emerald-700 dark:text-emerald-400"><span className="font-semibold">Retroalimentación:</span> {mySub.feedback}</p>
            </div>
          )}
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-bold text-foreground">Actividades</h2>
        {userRole === "Profesor" && (
          <button onClick={() => setShowCreate(true)} className="flex items-center gap-2 px-4 py-2 rounded-xl bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors cursor-pointer">
            <Plus className="h-4 w-4" />
            Nueva actividad
          </button>
        )}
      </div>

      {tasks.length > 0 && (
        <div className="bg-card border border-border rounded-2xl overflow-hidden">
          <div className="px-5 py-3 border-b border-border">
            <h3 className="text-sm font-semibold text-foreground">Tareas y Evaluaciones</h3>
          </div>
          <div className="divide-y divide-border">{tasks.map(renderActivity)}</div>
        </div>
      )}

      {activities.length === 0 && (
        <div className="bg-card border border-border rounded-2xl p-12 text-center">
          <ClipboardList className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm font-semibold text-foreground">Sin actividades</p>
          {userRole === "Profesor" && <p className="text-xs text-muted-foreground mt-1">Crea la primera actividad para este curso.</p>}
        </div>
      )}

      {showCreate && <CourseActivityModal sectionId={sectionId} onClose={() => setShowCreate(false)} onSuccess={() => { setShowCreate(false); qc.invalidateQueries({ queryKey: ["course", sectionId, "activities"] }) }} />}
      {editingId && <CourseActivityModal sectionId={sectionId} activityId={editingId} existing={activities.find((a) => a.id === editingId)} onClose={() => setEditingId(null)} onSuccess={() => { setEditingId(null); qc.invalidateQueries({ queryKey: ["course", sectionId, "activities"] }) }} />}
      {submittingId && <SubmitModal activityId={submittingId} activity={activities.find((a) => a.id === submittingId)!} existing={mySubmissions.find((s) => s.activityId === submittingId)} onClose={() => setSubmittingId(null)} onSuccess={() => { setSubmittingId(null); qc.invalidateQueries({ queryKey: ["course", sectionId, "my-submissions"] }) }} />}
      {gradingSubmissions && <GradingModal activityId={gradingSubmissions} activity={activities.find((a) => a.id === gradingSubmissions)!} onClose={() => setGradingSubmissions(null)} onSuccess={() => qc.invalidateQueries({ queryKey: ["course", sectionId] })} />}
      {deleteConfirmId && <DeleteConfirmModal title="Eliminar actividad" onConfirm={() => deleteMutation.mutate(deleteConfirmId)} onCancel={() => setDeleteConfirmId(null)} loading={deleteMutation.isPending} />}
    </div>
  )
}

export function CourseActivityModal({
  sectionId, activityId, existing, defaultType = "Tarea", onClose, onSuccess,
}: {
  sectionId: string
  activityId?: string
  existing?: ActivityDto
  defaultType?: ActivityType
  onClose: () => void
  onSuccess: () => void
}) {
  const [title, setTitle] = useState(existing?.title ?? "")
  const [description, setDescription] = useState(existing?.description ?? "")
  const [type, setType] = useState<ActivityType>(existing?.type as ActivityType ?? defaultType)
  const [dueDate, setDueDate] = useState(existing?.dueDate ? existing.dueDate.slice(0, 16) : "")
  const [maxScore, setMaxScore] = useState(existing?.maxScore?.toString() ?? "10")
  const [weight, setWeight] = useState(existing?.weight?.toString() ?? "10")
  const [resourceUrl, setResourceUrl] = useState(existing?.resourceUrl ?? "")
  const [isPublished, setIsPublished] = useState(existing?.isPublished ?? true)
  const [error, setError] = useState("")

  const createMutation = useMutation({
    mutationFn: (data: CreateActivityDto) => coursesApi.createActivity(sectionId, data),
    onSuccess,
    onError: (e) => setError(getErrorMessage(e)),
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateActivityDto) => coursesApi.updateActivity(activityId!, { ...data, isPublished }),
    onSuccess,
    onError: (e) => setError(getErrorMessage(e)),
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const data: CreateActivityDto = {
      title, description, type,
      dueDate: dueDate ? new Date(dueDate).toISOString() : null,
      maxScore: parseFloat(maxScore),
      weight: parseFloat(weight),
      isPublished,
      resourceUrl: resourceUrl || null,
    }
    if (activityId) updateMutation.mutate(data)
    else createMutation.mutate(data)
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <ModalOverlay onClose={onClose}>
      <form onSubmit={handleSubmit} className="space-y-4">
        <h2 className="text-lg font-bold text-foreground">{activityId ? "Editar actividad" : "Nueva actividad"}</h2>
        {error && <p className="text-xs text-red-500 bg-red-500/10 px-3 py-2 rounded-lg">{error}</p>}
        <Field label="Título"><input value={title} onChange={(e) => setTitle(e.target.value)} required className={inputCls} /></Field>
        <Field label="Descripción"><textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} className={`${inputCls} resize-none`} /></Field>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Tipo">
            <select value={type} onChange={(e) => setType(e.target.value as ActivityType)} className={inputCls}>
              {ACTIVITY_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </Field>
          <Field label="Fecha límite"><input type="datetime-local" value={dueDate} onChange={(e) => setDueDate(e.target.value)} className={inputCls} /></Field>
        </div>
        <div className="grid grid-cols-2 gap-3">
          <Field label="Puntuación máxima"><input type="number" min="0" step="0.5" value={maxScore} onChange={(e) => setMaxScore(e.target.value)} required className={inputCls} /></Field>
          <Field label="Ponderación (%)"><input type="number" min="0" max="100" value={weight} onChange={(e) => setWeight(e.target.value)} required className={inputCls} /></Field>
        </div>
        {type === "Recurso" && <Field label="URL del recurso"><input value={resourceUrl} onChange={(e) => setResourceUrl(e.target.value)} placeholder="https://..." className={inputCls} /></Field>}
        <label className="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" checked={isPublished} onChange={(e) => setIsPublished(e.target.checked)} className="rounded" />
          <span className="text-sm text-foreground">Publicar inmediatamente</span>
        </label>
        <div className="flex gap-2 pt-2">
          <button type="button" onClick={onClose} className={`flex-1 ${cancelBtnCls}`}>Cancelar</button>
          <button type="submit" disabled={isPending} className={`flex-1 ${primaryBtnCls}`}>{isPending ? "Guardando..." : activityId ? "Actualizar" : "Crear"}</button>
        </div>
      </form>
    </ModalOverlay>
  )
}

function SubmitModal({
  activityId, activity, existing, onClose, onSuccess,
}: {
  activityId: string
  activity: ActivityDto
  existing?: StudentSubmissionDto
  onClose: () => void
  onSuccess: () => void
}) {
  const [content, setContent] = useState(existing?.content ?? "")
  const [error, setError] = useState("")

  const submitMutation = useMutation({
    mutationFn: () => coursesApi.submit(activityId, { content }),
    onSuccess,
    onError: (e) => setError(getErrorMessage(e)),
  })

  const isClosed = existing?.status === "Cerrada"

  return (
    <ModalOverlay onClose={onClose}>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-foreground">{activity.title}</h2>
          <p className="text-xs text-muted-foreground mt-0.5">{activity.type} · {activity.maxScore} pts · Pond. {activity.weight}%</p>
        </div>
        {activity.dueDate && (
          <div className={`text-xs px-3 py-2 rounded-lg ${new Date(activity.dueDate) < new Date() ? "bg-red-500/10 text-red-500" : "bg-primary/5 text-primary"}`}>
            {new Date(activity.dueDate) < new Date() ? "Venció" : "Vence"}: {new Date(activity.dueDate).toLocaleDateString("es-DO", { year: "numeric", month: "long", day: "numeric", hour: "2-digit", minute: "2-digit" })}
          </div>
        )}
        <p className="text-sm text-muted-foreground">{activity.description}</p>
        {existing?.score !== undefined && existing.score !== null && (
          <div className="p-3 rounded-xl bg-emerald-500/5 border border-emerald-500/20 space-y-1">
            <p className="text-sm font-semibold text-emerald-700 dark:text-emerald-400">Calificación: {existing.score}/{activity.maxScore}</p>
            {existing.feedback && <p className="text-xs text-emerald-600/70 dark:text-emerald-500/70">{existing.feedback}</p>}
          </div>
        )}
        {error && <p className="text-xs text-red-500 bg-red-500/10 px-3 py-2 rounded-lg">{error}</p>}
        {!isClosed && (
          <>
            <Field label="Tu entrega">
              <textarea value={content} onChange={(e) => setContent(e.target.value)} rows={5} placeholder="Escribe tu respuesta o pega el enlace a tu trabajo..." className={`${inputCls} resize-none`} />
            </Field>
            <div className="flex gap-2 pt-2">
              <button type="button" onClick={onClose} className={`flex-1 ${cancelBtnCls}`}>Cerrar</button>
              <button onClick={() => submitMutation.mutate()} disabled={submitMutation.isPending} className={`flex-1 ${primaryBtnCls}`}>
                {submitMutation.isPending ? "Enviando..." : existing ? "Actualizar entrega" : "Entregar"}
              </button>
            </div>
          </>
        )}
        {isClosed && <p className="text-sm text-center text-muted-foreground">Esta actividad está cerrada.</p>}
        {isClosed && <button onClick={onClose} className={`w-full ${cancelBtnCls}`}>Cerrar</button>}
      </div>
    </ModalOverlay>
  )
}

function GradingModal({
  activityId, activity, onClose, onSuccess,
}: {
  activityId: string
  activity: ActivityDto
  onClose: () => void
  onSuccess: () => void
}) {
  const qc = useQueryClient()
  const { data: submissions = [], isLoading } = useQuery({
    queryKey: ["activity", activityId, "submissions"],
    queryFn: () => coursesApi.getSubmissions(activityId),
  })

  const [scores, setScores] = useState<Record<string, string>>({})
  const [feedbacks, setFeedbacks] = useState<Record<string, string>>({})
  const [saving, setSaving] = useState<string | null>(null)
  const [errors, setErrors] = useState<Record<string, string>>({})

  const gradeOne = async (subId: string) => {
    const score = parseFloat(scores[subId] ?? "")
    if (isNaN(score)) { setErrors((e) => ({ ...e, [subId]: "Nota inválida" })); return }
    setSaving(subId)
    try {
      await coursesApi.gradeSubmission(subId, { score, feedback: feedbacks[subId] ?? "" })
      qc.invalidateQueries({ queryKey: ["activity", activityId, "submissions"] })
      onSuccess()
      setErrors((e) => { const next = { ...e }; delete next[subId]; return next })
    } catch (e) {
      setErrors((prev) => ({ ...prev, [subId]: getErrorMessage(e) }))
    } finally {
      setSaving(null)
    }
  }

  return (
    <ModalOverlay onClose={onClose} wide>
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-foreground">Entregas — {activity.title}</h2>
          <p className="text-xs text-muted-foreground mt-0.5">Puntuación máxima: {activity.maxScore}</p>
        </div>
        {isLoading ? <Spinner /> : submissions.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">Sin entregas</p>
        ) : (
          <div className="space-y-3 max-h-[60vh] overflow-y-auto pr-1">
            {submissions.map((sub) => (
              <div key={sub.id} className="border border-border rounded-xl p-4 space-y-3">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-foreground">{sub.studentName}</p>
                    <p className="text-xs text-muted-foreground">{sub.studentCode}</p>
                  </div>
                  <div className="text-right">
                    <span className={`text-xs font-medium ${statusColor(sub.status)}`}>{sub.status}</span>
                    {sub.submittedAt && <p className="text-xs text-muted-foreground">{new Date(sub.submittedAt).toLocaleDateString("es-DO")}</p>}
                  </div>
                </div>
                {sub.content && <div className="p-3 rounded-lg bg-muted/50 text-xs text-foreground whitespace-pre-wrap max-h-24 overflow-y-auto">{sub.content}</div>}
                <div className="grid grid-cols-[auto_1fr_auto] gap-2 items-end">
                  <div>
                    <label className="text-xs text-muted-foreground block mb-1">Nota</label>
                    <input type="number" min="0" max={activity.maxScore} step="0.5" defaultValue={sub.score?.toString() ?? ""} onChange={(e) => setScores((s) => ({ ...s, [sub.id]: e.target.value }))} className={`w-20 ${inputCls}`} />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground block mb-1">Retroalimentación</label>
                    <input defaultValue={sub.feedback ?? ""} onChange={(e) => setFeedbacks((f) => ({ ...f, [sub.id]: e.target.value }))} placeholder="Opcional..." className={inputCls} />
                  </div>
                  <button onClick={() => gradeOne(sub.id)} disabled={saving === sub.id} className={`${primaryBtnCls} px-3 py-2 text-xs`}>
                    {saving === sub.id ? "..." : "Guardar"}
                  </button>
                </div>
                {errors[sub.id] && <p className="text-xs text-red-500">{errors[sub.id]}</p>}
              </div>
            ))}
          </div>
        )}
        <button onClick={onClose} className={`w-full ${cancelBtnCls}`}>Cerrar</button>
      </div>
    </ModalOverlay>
  )
}

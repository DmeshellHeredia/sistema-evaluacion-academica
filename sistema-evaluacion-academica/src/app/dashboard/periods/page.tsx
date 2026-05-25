"use client"

import { useState } from "react"
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query"
import { CalendarDays, Plus, Pencil, Trash2, Loader2, Lock, Unlock, CheckCircle2 } from "lucide-react"
import { toast } from "sonner"
import { useForm } from "react-hook-form"
import { periodsApi } from "@/lib/api"
import { onMutationError } from "@/lib/mutation-error"
import { useAuth } from "@/context/auth-context"
import type { AcademicPeriodDto, CreateAcademicPeriodDto, UpdateAcademicPeriodDto } from "@/types"

type PeriodForm = {
  name: string
  code: string
  startDate: string
  endDate: string
}

export default function PeriodsPage() {
  const { user } = useAuth()
  const qc = useQueryClient()
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<AcademicPeriodDto | null>(null)
  const [pendingId, setPendingId] = useState<string | null>(null)

  const { data: periods = [], isLoading } = useQuery({
    queryKey: ["periods"],
    queryFn: periodsApi.getAll,
  })

  const form = useForm<PeriodForm>({
    defaultValues: { name: "", code: "", startDate: "", endDate: "" },
  })

  const invalidate = () => {
    void qc.invalidateQueries({ queryKey: ["periods"] })
    void qc.invalidateQueries({ queryKey: ["enrollment-status"] })
  }

  const createMutation = useMutation({
    mutationFn: (dto: CreateAcademicPeriodDto) => periodsApi.create(dto),
    onSuccess: () => { invalidate(); setShowForm(false); form.reset(); toast.success("Período creado") },
    onError: onMutationError,
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: UpdateAcademicPeriodDto }) => periodsApi.update(id, dto),
    onSuccess: () => { invalidate(); setEditing(null); form.reset(); toast.success("Período actualizado") },
    onError: onMutationError,
  })

  const deleteMutation = useMutation({
    mutationFn: (id: string) => periodsApi.delete(id),
    onMutate: (id) => setPendingId(id),
    onSettled: () => setPendingId(null),
    onSuccess: () => { invalidate(); toast.success("Período eliminado") },
    onError: onMutationError,
  })

  const openMutation = useMutation({
    mutationFn: (id: string) => periodsApi.open(id),
    onMutate: (id) => setPendingId(id),
    onSettled: () => setPendingId(null),
    onSuccess: () => { invalidate(); toast.success("Selección de materias activada") },
    onError: onMutationError,
  })

  const closeMutation = useMutation({
    mutationFn: (id: string) => periodsApi.close(id),
    onMutate: (id) => setPendingId(id),
    onSettled: () => setPendingId(null),
    onSuccess: () => { invalidate(); toast.success("Selección de materias desactivada") },
    onError: onMutationError,
  })

  function openNew() {
    setEditing(null)
    form.reset({ name: "", code: "", startDate: "", endDate: "" })
    setShowForm(true)
  }

  function openEdit(p: AcademicPeriodDto) {
    setEditing(p)
    form.reset({
      name: p.name,
      code: p.code,
      startDate: p.startDate.slice(0, 10),
      endDate: p.endDate.slice(0, 10),
    })
    setShowForm(true)
  }

  function onSubmit(data: PeriodForm) {
    const dto = { ...data, startDate: new Date(data.startDate).toISOString(), endDate: new Date(data.endDate).toISOString() }
    if (editing) {
      updateMutation.mutate({ id: editing.id, dto })
    } else {
      createMutation.mutate(dto)
    }
  }

  const activePeriod = periods.find((p) => p.isEnrollmentOpen)
  const isMutating = createMutation.isPending || updateMutation.isPending

  if (user?.role !== "Admin")
    return <div className="p-8 text-center text-muted-foreground">Esta página es solo para administradores.</div>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-lg font-bold text-foreground">Períodos Académicos</h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Gestiona los períodos y controla la selección de materias
          </p>
        </div>
        <button
          onClick={openNew}
          className="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 transition-colors cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          Nuevo período
        </button>
      </div>

      {activePeriod ? (
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-green-500/10 border border-green-500/20 text-green-600 dark:text-green-400">
          <CheckCircle2 className="h-4 w-4 shrink-0" />
          <p className="text-sm font-medium">
            Selección activa — <span className="font-bold">{activePeriod.name}</span> ({activePeriod.code})
          </p>
        </div>
      ) : (
        <div className="flex items-center gap-3 px-4 py-3 rounded-xl bg-muted border border-border text-muted-foreground">
          <Lock className="h-4 w-4 shrink-0" />
          <p className="text-sm font-medium">La selección de materias está desactivada</p>
        </div>
      )}

      {showForm && (
        <div className="bg-card border border-border rounded-2xl p-5">
          <h2 className="text-sm font-semibold text-foreground mb-4">
            {editing ? "Editar período" : "Nuevo período"}
          </h2>
          <form onSubmit={form.handleSubmit(onSubmit)} className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label htmlFor="period-name" className="block text-xs font-medium text-foreground mb-1">Nombre</label>
              <input
                id="period-name"
                {...form.register("name", { required: true })}
                placeholder="Cuatrimestre I 2026"
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label htmlFor="period-code" className="block text-xs font-medium text-foreground mb-1">Código</label>
              <input
                id="period-code"
                {...form.register("code", { required: true })}
                placeholder="2026-1"
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label htmlFor="period-start" className="block text-xs font-medium text-foreground mb-1">Fecha de inicio</label>
              <input
                id="period-start"
                type="date"
                {...form.register("startDate", { required: true })}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring cursor-pointer"
              />
            </div>
            <div>
              <label htmlFor="period-end" className="block text-xs font-medium text-foreground mb-1">Fecha de fin</label>
              <input
                id="period-end"
                type="date"
                {...form.register("endDate", { required: true })}
                className="w-full rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring cursor-pointer"
              />
            </div>
            <div className="sm:col-span-2 flex gap-2 justify-end">
              <button
                type="button"
                onClick={() => { setShowForm(false); setEditing(null) }}
                className="px-4 py-2 rounded-xl border border-border text-sm text-muted-foreground hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={isMutating}
                className="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer"
              >
                {isMutating && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {editing ? "Guardar cambios" : "Crear período"}
              </button>
            </div>
          </form>
        </div>
      )}

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2].map((i) => (
            <div key={i} className="bg-card border border-border rounded-2xl p-4 h-24 animate-pulse" />
          ))}
        </div>
      ) : periods.length === 0 ? (
        <div className="bg-card border border-border rounded-2xl p-14 text-center">
          <CalendarDays className="h-8 w-8 text-muted-foreground mx-auto mb-3" />
          <p className="text-sm text-muted-foreground">No hay períodos académicos</p>
        </div>
      ) : (
        <div className="space-y-3">
          {periods.map((p) => (
            <PeriodCard
              key={p.id}
              period={p}
              pending={pendingId === p.id}
              onEdit={() => openEdit(p)}
              onDelete={() => deleteMutation.mutate(p.id)}
              onOpen={() => openMutation.mutate(p.id)}
              onClose={() => closeMutation.mutate(p.id)}
            />
          ))}
        </div>
      )}
    </div>
  )
}

function PeriodCard({
  period,
  pending,
  onEdit,
  onDelete,
  onOpen,
  onClose,
}: {
  period: AcademicPeriodDto
  pending: boolean
  onEdit: () => void
  onDelete: () => void
  onOpen: () => void
  onClose: () => void
}) {
  const fmt = (s: string) =>
    new Date(s).toLocaleDateString("es-ES", { year: "numeric", month: "short", day: "numeric" })

  return (
    <div className={`bg-card border rounded-2xl p-4 flex items-center gap-4 transition-shadow hover:shadow-sm ${period.isEnrollmentOpen ? "border-green-500/30" : "border-border"}`}>
      <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${period.isEnrollmentOpen ? "bg-green-500/10 text-green-500" : "bg-muted text-muted-foreground"}`}>
        {period.isEnrollmentOpen ? <Unlock className="h-4 w-4" /> : <Lock className="h-4 w-4" />}
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <p className="font-semibold text-foreground text-sm">{period.name}</p>
          <span className="text-xs text-muted-foreground font-mono">({period.code})</span>
          {period.isEnrollmentOpen && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-500/10 text-green-600 dark:text-green-400">
              <CheckCircle2 className="h-3 w-3" />
              Activo
            </span>
          )}
        </div>
        <p className="text-xs text-muted-foreground mt-0.5">
          {fmt(period.startDate)} — {fmt(period.endDate)}
        </p>
      </div>

      <div className="flex items-center gap-1.5 shrink-0">
        {period.isEnrollmentOpen ? (
          <button
            onClick={onClose}
            disabled={pending}
            className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-medium bg-amber-500/10 text-amber-600 hover:bg-amber-500/20 disabled:opacity-50 transition-colors cursor-pointer"
          >
            {pending ? <Loader2 className="h-3 w-3 animate-spin" /> : <Lock className="h-3 w-3" />}
            Desactivar
          </button>
        ) : (
          <button
            onClick={onOpen}
            disabled={pending}
            className="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg text-xs font-medium bg-green-500/10 text-green-600 hover:bg-green-500/20 disabled:opacity-50 transition-colors cursor-pointer"
          >
            {pending ? <Loader2 className="h-3 w-3 animate-spin" /> : <Unlock className="h-3 w-3" />}
            Activar
          </button>
        )}
        <button
          onClick={onEdit}
          disabled={pending}
          className="p-1.5 rounded-lg text-muted-foreground hover:text-foreground hover:bg-muted disabled:opacity-50 transition-colors cursor-pointer"
          aria-label={`Editar ${period.name}`}
        >
          <Pencil className="h-3.5 w-3.5" />
        </button>
        <button
          onClick={onDelete}
          disabled={pending || period.isEnrollmentOpen}
          className="p-1.5 rounded-lg text-muted-foreground hover:text-red-500 hover:bg-red-500/10 disabled:opacity-30 transition-colors cursor-pointer"
          aria-label={`Eliminar ${period.name}`}
          title={period.isEnrollmentOpen ? "No se puede eliminar un período activo" : undefined}
        >
          <Trash2 className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  )
}

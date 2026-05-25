"use client"

import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { Loader2 } from "lucide-react"
import { Modal, Field, inputCls } from "@/components/modal"
import { editSchema, type EditForm } from "./shared"

export interface EditDeleteModalsProps {
  modal: "edit" | "delete" | null
  selectedGrade: { id: string; subjectName: string; value: number; comments: string | null } | null
  onClose: () => void
  onEditSubmit: (data: EditForm) => void
  onDelete: () => void
  updatePending: boolean
  deletePending: boolean
  isAdmin?: boolean
}

export function EditDeleteModals({
  modal,
  selectedGrade,
  onClose,
  onEditSubmit,
  onDelete,
  updatePending,
  deletePending,
  isAdmin,
}: EditDeleteModalsProps) {
  const editForm = useForm<EditForm>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      value: selectedGrade?.value ?? 0,
      comments: selectedGrade?.comments ?? "",
    },
  })

  return (
    <>
      {modal === "edit" && selectedGrade && (
        <Modal title={`Editar: ${selectedGrade.subjectName}`} onClose={onClose} size="sm">
          <form onSubmit={editForm.handleSubmit(onEditSubmit)} className="space-y-4">
            <Field label="Calificación (0–10)" error={editForm.formState.errors.value?.message}>
              <input
                {...editForm.register("value", { valueAsNumber: true })}
                type="number"
                step="0.1"
                min={0}
                max={10}
                className={inputCls}
              />
            </Field>
            <Field label="Comentarios (opcional)" error={editForm.formState.errors.comments?.message}>
              <textarea
                {...editForm.register("comments")}
                rows={2}
                className={`${inputCls} h-auto py-2 resize-none`}
              />
            </Field>
            <div className="flex gap-3 pt-1">
              <button
                type="button"
                onClick={onClose}
                className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                type="submit"
                disabled={updatePending}
                className="flex-1 h-9 rounded-lg bg-primary text-primary-foreground text-sm font-semibold hover:bg-primary/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {updatePending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {updatePending ? "Guardando..." : "Guardar"}
              </button>
            </div>
          </form>
        </Modal>
      )}

      {modal === "delete" && selectedGrade && isAdmin && (
        <Modal title="Eliminar calificación" onClose={onClose} size="sm">
          <div className="space-y-5">
            <p className="text-sm text-muted-foreground leading-relaxed">
              ¿Eliminar la calificación de{" "}
              <span className="font-semibold text-foreground">{selectedGrade.subjectName}</span>{" "}
              ({selectedGrade.value.toFixed(1)})?
            </p>
            <div className="flex gap-3">
              <button
                onClick={onClose}
                className="flex-1 h-9 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors cursor-pointer"
              >
                Cancelar
              </button>
              <button
                onClick={onDelete}
                disabled={deletePending}
                className="flex-1 h-9 rounded-lg bg-destructive text-white text-sm font-semibold hover:bg-destructive/90 disabled:opacity-50 transition-colors cursor-pointer flex items-center justify-center gap-2"
              >
                {deletePending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
                {deletePending ? "Eliminando..." : "Eliminar"}
              </button>
            </div>
          </div>
        </Modal>
      )}
    </>
  )
}

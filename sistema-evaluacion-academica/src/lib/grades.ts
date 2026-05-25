export function gradeCategory(value: number): string {
  if (value >= 9) return "Excelente"
  if (value >= 7) return "Buena"
  return "Por mejorar"
}

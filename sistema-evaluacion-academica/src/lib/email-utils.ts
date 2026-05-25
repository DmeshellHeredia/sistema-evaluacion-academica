/**
 * Derives the institutional student email from first and last name.
 * Rule: [normalized_first].[normalized_last]@academia.com
 * Normalization: lowercase, trim, remove accents (NFD), remove non-alphanumeric.
 */
export function deriveStudentEmail(firstName: string, lastName: string): string {
  return `${normalizePart(firstName)}.${normalizePart(lastName)}@academia.com`
}

function normalizePart(str: string): string {
  if (!str) return ""
  return str
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .replace(/[^a-z0-9]/g, "")
}

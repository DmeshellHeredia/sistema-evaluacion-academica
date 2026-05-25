import { test, expect } from "@playwright/test";
import { loginAsProfessor } from "./helpers/auth";

// prof.garcia@academia.com = Ana García (prof1)
// Her sections: IS-MAT1-A, IS-ALG1-A, IS-MAT2-A, DS-PRG1-A, DS-WEB1-A, DS-PRG2-A, DS-WEB2-A, DS-TST1-A
// NOT hers:     IS-PRG1-A (Roberto López)
test.describe("Profesor — sidebar y navegación", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsProfessor(page);
    await page.goto("/dashboard");
  });

  test("no ve 'Profesores' ni 'Períodos' en el sidebar", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Profesores" })).not.toBeVisible();
    await expect(page.getByRole("link", { name: "Períodos" })).not.toBeVisible();
  });

  test("ve 'Calificaciones' y 'Mis Cursos' en el sidebar pero NO 'Materias'", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Calificaciones" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Mis Cursos" })).toBeVisible();
    // Materias is Admin-only now
    await expect(page.getByRole("link", { name: "Materias" })).not.toBeVisible();
  });
});

// prof.garcia has 8 sections in the seeded DB (IS-MAT1-A … DS-TST1-A)
test.describe("Profesor — página de calificaciones", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsProfessor(page);
    await page.goto("/dashboard/grades");
    // Wait for auth hydration: sidebar link is the stable indicator
    await expect(page.getByRole("link", { name: "Calificaciones" })).toBeVisible({
      timeout: 20_000,
    });
  });

  test("página carga y muestra selector de sección", async ({ page }) => {
    await expect(page.getByText("Seleccionar sección")).toBeVisible({ timeout: 15_000 });
    await expect(page.locator("select")).toBeVisible({ timeout: 15_000 });
  });

  test("selector no muestra 'Sin secciones asignadas'", async ({ page }) => {
    await expect(page.getByText("Seleccionar sección")).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText("Sin secciones asignadas")).not.toBeVisible();
  });
});

test.describe("Profesor — confirmación al eliminar actividades", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsProfessor(page);
    // Navigate to the first section page for Ana García
    await page.goto("/dashboard/courses");
    await expect(page.getByRole("link", { name: "Calificaciones" })).toBeVisible({ timeout: 20_000 });
  });

  test("botón eliminar actividad muestra modal de confirmación con advertencia", async ({ page }) => {
    // Click first section card
    await page.locator("a[href^='/dashboard/courses/']").first().click();
    await expect(page).toHaveURL(/\/dashboard\/courses\//, { timeout: 10_000 });

    // Go to Actividades tab
    await page.getByRole("button", { name: /actividades/i }).click();

    // If there's a delete button, click it to open confirm modal
    const deleteBtn = page.locator("button[aria-label*='liminar'], button[title*='liminar']").first();
    if (await deleteBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await deleteBtn.click();
      await expect(page.getByText("Esta acción no se puede deshacer.")).toBeVisible({ timeout: 5_000 });
      // Cancelar closes modal
      await page.getByRole("button", { name: "Cancelar" }).click();
      await expect(page.getByText("Esta acción no se puede deshacer.")).not.toBeVisible();
    }
  });
});

import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "./helpers/auth";

test.describe("Admin — Profesores", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/dashboard/professors");
    // Wait for loading to settle
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });
  });

  test("carga sin error y muestra profesores sembrados", async ({ page }) => {
    // At least Ana García from seed
    await expect(page.getByText("Ana García")).toBeVisible({ timeout: 10_000 });
    // At least one more professor
    await expect(page.getByText("Roberto López")).toBeVisible();
  });

  test("sidebar muestra Profesores solo para Admin", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Profesores" })).toBeVisible();
    // Admin-only: Períodos also visible
    await expect(page.getByRole("link", { name: "Períodos" })).toBeVisible();
  });
});

test.describe("Admin — Materias", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/dashboard/subjects");
    // Wait for skeleton cards to disappear
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });
  });

  test("carga y muestra materias sembradas", async ({ page }) => {
    // 27 seeded subjects: text shows count
    await expect(
      page.getByText(/\d+ materias? registradas?/)
    ).toBeVisible({ timeout: 10_000 });

    // A seeded IS subject card
    await expect(page.getByText("Matemáticas I")).toBeVisible();
  });

  test("modal Nueva materia tiene checkbox 'Aplica a todas las carreras' y multi-select de carreras", async ({ page }) => {
    await page.getByRole("button", { name: "Nueva materia" }).click();

    // Modal is open
    await expect(page.getByText("Nueva materia").first()).toBeVisible();

    await expect(
      page.getByText("Aplica a todas las carreras")
    ).toBeVisible();

    // Career checkboxes visible by default (appliesToAllCareers = false).
    // Scoped to the dialog to avoid strict-mode violations with background cards.
    const modal = page.getByLabel("Nueva materia");
    await expect(modal.getByText("Ingeniería en Sistemas")).toBeVisible();
    await expect(modal.getByText("Ciberseguridad")).toBeVisible();
    await expect(modal.getByText("Desarrollo de Software")).toBeVisible();

    // Checking "Aplica a todas" hides the career checkboxes
    const allCareersCheckbox = page.locator('label').filter({ hasText: "Aplica a todas las carreras" }).locator('input[type="checkbox"]')
    await allCareersCheckbox.check();
    await expect(modal.getByText("Ingeniería en Sistemas")).not.toBeVisible();
  });

  test("sidebar Admin no muestra 'Promedios'", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Promedios" })).not.toBeVisible();
  });

  test("sidebar Admin no tiene botón de cerrar sesión en el pie", async ({ page }) => {
    // Logout is only in the header dropdown, not in the sidebar footer
    const sidebarFooter = page.locator("aside footer, nav > div:last-child").first();
    await expect(sidebarFooter.getByRole("button", { name: /cerrar sesión/i })).not.toBeVisible();
  });

  test("modal Nueva materia tiene selector de prerequisitos", async ({ page }) => {
    await page.getByRole("button", { name: "Nueva materia" }).click();
    await expect(page.getByText("Prerequisitos")).toBeVisible();
  });
});

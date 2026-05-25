import type { Page } from "@playwright/test";

const API_URL = process.env.PLAYWRIGHT_API_URL ?? "http://localhost:5000";

interface LoginPayload {
  token: string;
  email: string;
  fullName: string;
  role: string;
  expiresAt: string;
}

async function loginAs(page: Page, email: string, password: string): Promise<void> {
  const response = await page.request.post(`${API_URL}/api/auth/login`, {
    data: { email, password },
  });

  if (!response.ok()) {
    const body = await response.text().catch(() => "");
    throw new Error(`Login failed for ${email} (HTTP ${response.status()}): ${body}`);
  }

  const data = await response.json() as LoginPayload;

  if (!data.token) {
    throw new Error(`Login response missing token for ${email}: ${JSON.stringify(data)}`);
  }

  const payloadPart = data.token.split(".")[1];
  const payload = JSON.parse(
    Buffer.from(payloadPart, "base64url").toString("utf-8")
  ) as Record<string, string>;

  const authUser = {
    id: payload["nameid"] ?? payload["sub"] ?? "",
    email: data.email,
    fullName: data.fullName,
    role: data.role,
    expiresAt: data.expiresAt,
  };

  await page.goto("/login");
  await page.evaluate(
    ({ token, user }) => {
      localStorage.setItem("academia_token", token);
      localStorage.setItem("academia_user", JSON.stringify(user));
    },
    { token: data.token, user: authUser }
  );
}

export async function loginAsAdmin(page: Page): Promise<void> {
  return loginAs(page, "admin@academia.com", "Admin123!");
}

// prof.garcia@academia.com = Ana García (prof1)
// Sections: IS-MAT1-A, IS-ALG1-A, IS-MAT2-A, DS-PRG1-A, DS-WEB1-A, DS-PRG2-A, DS-WEB2-A, DS-TST1-A
export async function loginAsProfessor(
  page: Page,
  email = "prof.garcia@academia.com"
): Promise<void> {
  return loginAs(page, email, "Profesor123!");
}

// Default: juan.perez@academia.com — IS sem 3
export async function loginAsStudent(
  page: Page,
  email = "juan.perez@academia.com"
): Promise<void> {
  return loginAs(page, email, "Estudiante123!");
}

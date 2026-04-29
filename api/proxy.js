function normalizeOrigin(origin) {
  if (!origin) return null;
  return origin.endsWith("/") ? origin.slice(0, -1) : origin;
}

function getForwardBody(req) {
  if (req.method === "GET" || req.method === "HEAD") return undefined;

  if (req.body == null) return undefined;
  if (Buffer.isBuffer(req.body) || typeof req.body === "string") return req.body;

  const contentType = String(req.headers["content-type"] || "").toLowerCase();

  if (contentType.includes("application/x-www-form-urlencoded")) {
    return new URLSearchParams(req.body).toString();
  }

  if (contentType.includes("application/json")) {
    return JSON.stringify(req.body);
  }

  return JSON.stringify(req.body);
}

module.exports = async (req, res) => {
  const backendOrigin = normalizeOrigin(
    process.env.BACKEND_ORIGIN || process.env.BACKEND_URL
  );

  if (!backendOrigin) {
    return res.status(500).json({
      error:
        "BACKEND_ORIGIN nao configurada. Defina essa variavel no projeto da Vercel.",
    });
  }

  const pathParam = req.query.path;
  const path = Array.isArray(pathParam)
    ? pathParam.join("/")
    : pathParam || "";

  const targetUrl = new URL(`${backendOrigin}/${path}`);

  for (const [key, value] of Object.entries(req.query)) {
    if (key === "path") continue;

    if (Array.isArray(value)) {
      for (const item of value) {
        targetUrl.searchParams.append(key, String(item));
      }
      continue;
    }

    if (value != null) {
      targetUrl.searchParams.append(key, String(value));
    }
  }

  const headers = { ...req.headers };
  delete headers.host;
  delete headers.connection;
  delete headers["content-length"];

  headers["x-forwarded-host"] = req.headers.host || "";
  headers["x-forwarded-proto"] = "https";

  const body = getForwardBody(req);

  try {
    const upstream = await fetch(targetUrl.toString(), {
      method: req.method,
      headers,
      body,
      redirect: "manual",
    });

    res.status(upstream.status);

    upstream.headers.forEach((value, key) => {
      const lower = key.toLowerCase();
      if (lower === "transfer-encoding") return;
      if (lower === "content-encoding") return;
      res.setHeader(key, value);
    });

    const buffer = Buffer.from(await upstream.arrayBuffer());
    return res.send(buffer);
  } catch (error) {
    return res.status(502).json({
      error: "Falha ao conectar no backend.",
      details: error instanceof Error ? error.message : "Erro desconhecido",
    });
  }
};

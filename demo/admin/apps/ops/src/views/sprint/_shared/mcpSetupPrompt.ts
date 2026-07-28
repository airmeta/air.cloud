const TOKEN_PLACEHOLDERS = [
  '{{agentToken}}',
  'Bearer <AgentSprint Agent Token>',
  'Bearer <这里填令牌>',
];

export function buildAgentsprintMcpSetupPrompt(content?: string) {
  return content || '';
}

export function buildAgentsprintMcpSetupPromptWithToken(content: string, bearerToken: string) {
  const replaced = TOKEN_PLACEHOLDERS.reduce(
    (result, placeholder) => result.replaceAll(placeholder, bearerToken),
    buildAgentsprintMcpSetupPrompt(content),
  );
  return replaced.replace(/Authorization\s*=\s*"[^"]*"/g, `Authorization = "${bearerToken}"`);
}

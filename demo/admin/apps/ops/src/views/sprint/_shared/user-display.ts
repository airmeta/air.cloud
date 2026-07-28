import type { SprintUserApi } from '#/api/sprint/mvp';

function buildUserLookup(users: SprintUserApi.UserOption[]) {
  return {
    byId: Object.fromEntries(users.map((item) => [item.id, item])),
    byUsername: Object.fromEntries(users.map((item) => [item.username, item])),
  };
}

function resolveUserName(
  userId: string | undefined,
  lookup: ReturnType<typeof buildUserLookup>,
  emptyText = '未指定',
) {
  if (!userId) return emptyText;
  const user = lookup.byId[userId] || lookup.byUsername[userId];
  return user?.displayName || user?.username || userId;
}

export { buildUserLookup, resolveUserName };

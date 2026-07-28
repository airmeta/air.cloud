import { beforeEach, describe, expect, it, vi } from 'vitest';

const dialogInstance = {
  hide: vi.fn(),
  setConfirmLoading: vi.fn(),
};
const confirm = vi.fn(() => dialogInstance);

vi.mock('#/adapter/tdesign', () => ({
  dialog: { confirm },
}));

describe('confirmLogout', () => {
  beforeEach(() => {
    confirm.mockClear();
    dialogInstance.hide.mockClear();
    dialogInstance.setConfirmLoading.mockClear();
  });

  it('waits for explicit confirmation before running logout', async () => {
    const onConfirm = vi.fn();
    const { confirmLogout } = await import('./confirm-logout');

    confirmLogout({ onConfirm });

    expect(onConfirm).not.toHaveBeenCalled();

    const options = (confirm.mock.calls[0] as any[])[0];
    await options.onConfirm();

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(dialogInstance.setConfirmLoading).toHaveBeenNthCalledWith(1, true);
    expect(dialogInstance.hide).toHaveBeenCalledTimes(1);
    expect(dialogInstance.setConfirmLoading).toHaveBeenLastCalledWith(false);
  });

  it('runs the cancel callback when the user cancels', async () => {
    const onCancel = vi.fn();
    const onConfirm = vi.fn();
    const { confirmLogout } = await import('./confirm-logout');

    confirmLogout({ onCancel, onConfirm });

    const options = (confirm.mock.calls[0] as any[])[0];
    options.onCancel();
    options.onClose();

    expect(onConfirm).not.toHaveBeenCalled();
    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(dialogInstance.hide).toHaveBeenCalledTimes(1);
  });
});

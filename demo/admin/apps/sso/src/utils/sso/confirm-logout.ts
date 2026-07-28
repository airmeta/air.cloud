import { dialog } from '#/adapter/tdesign';

type ConfirmLogoutOptions = {
  onCancel?: () => void;
  onConfirm: () => Promise<void> | void;
};

export function confirmLogout(options: ConfirmLogoutOptions) {
  let instance: ReturnType<typeof dialog.confirm>;
  let finished = false;
  instance = dialog.confirm({
    body: '\u786e\u8ba4\u9000\u51fa\u767b\u5f55\uff1f',
    cancelBtn: '\u53d6\u6d88',
    closeOnEscKeydown: false,
    closeOnOverlayClick: false,
    confirmBtn: '\u786e\u5b9a',
    header: '\u9000\u51fa\u767b\u5f55',
    onCancel: () => {
      finished = true;
      options.onCancel?.();
      instance.hide();
    },
    onClose: () => {
      if (!finished) {
        finished = true;
        options.onCancel?.();
      }
    },
    onConfirm: async () => {
      finished = true;
      instance.setConfirmLoading(true);
      try {
        await options.onConfirm();
        instance.hide();
      } finally {
        instance.setConfirmLoading(false);
      }
    },
  });

  return instance;
}

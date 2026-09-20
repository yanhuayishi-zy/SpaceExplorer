mergeInto(LibraryManager.library, {
  OpenImagePicker: function () {
    var input = document.getElementById('unity-avatar-input');
    if (!input) {
      input = document.createElement('input');
      input.type = 'file';
      input.accept = 'image/png,image/jpeg,image/jpg';
      input.id = 'unity-avatar-input';
      input.style.display = 'none';
      document.body.appendChild(input);
      input.addEventListener('change', function () {
        var file = this.files && this.files[0];
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function () {
          try {
            var bytes = new Uint8Array(reader.result);
            var bin = '';
            var chunk = 0x8000;
            for (var i = 0; i < bytes.length; i += chunk) {
              bin += String.fromCharCode.apply(null, bytes.subarray(i, i + chunk));
            }
            var b64 = btoa(bin);
            SendMessage('MobileGameShell', 'OnWebGLAvatarBase64', b64);
          } catch (e) {
            console.error('avatar read fail', e);
          }
        };
        reader.readAsArrayBuffer(file);
        input.value = '';
      });
    }
    input.click();
  }
});

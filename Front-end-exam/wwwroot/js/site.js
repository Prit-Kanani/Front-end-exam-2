document.addEventListener("DOMContentLoaded", () => {
  const sidenavs = document.querySelectorAll(".sidenav");
  const selects = document.querySelectorAll("select");
  if (window.M && sidenavs.length) {
    window.M.Sidenav.init(sidenavs);
  }
  if (window.M && selects.length) {
    window.M.FormSelect.init(selects);
  }
});

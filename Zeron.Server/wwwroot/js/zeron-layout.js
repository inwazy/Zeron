window.zeronLayout = {
    toggleSidebar: function () {
        document.querySelector('.page')?.classList.toggle('sidebar-open');
    },
    closeSidebar: function () {
        document.querySelector('.page')?.classList.remove('sidebar-open');
    }
};

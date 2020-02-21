(function($){
  $(function(){

    $('.button-collapse').sideNav();
    $('.carousel').carousel();

  }); // end of document ready
})(jQuery); // end of jQuery name space

function youtube_defer() {
  var iframes = document.querySelectorAll('.youtube');
  iframes.forEach(function(iframe){
    if(iframe.getAttribute('data-src')) {
      iframe.setAttribute('src',iframe.getAttribute('data-src'));
    }
  });
}
window.addEventListener('load', youtube_defer);

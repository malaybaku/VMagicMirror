$(function () {
  var posts = [];
  $.get('ja/api/search-source.json', function (data) {
    posts = data;
  });
  $('#search').on('keyup', function () {
    var keyword = this.value.toLowerCase();
    var searchResult = [];

    if (keyword.length > 0) {
      $('#search-result').show();
    } else {
      $('#search-result').hide();
    }
    $('.result-item').remove();
    for (var i = 0; i < posts.length; i++) {
      var post = posts[i];
      if (
        post.title.toLowerCase().indexOf(keyword) >= 0 ||
        post.content.toLowerCase().indexOf(keyword) >= 0
      ) {
        searchResult.push(post);
      }
    }
    if (searchResult.length === 0) {
      $('#search-result').append(
        '<div class="result-item"><div class="description">There is no search result.</div></div>'
      );
    } else {
      for (var i = 0; i < searchResult.length; i++) {
        $('#search-result').append(
          '<a class="result-item" href="' +
            searchResult[i].url +
            '"><div class="title">【' + searchResult[i].category + '】' +
            searchResult[i].title +
            '</div><div class="description">' +
            searchResult[i].description +
            '</div></a>'
        );
      }
    }
  });
});

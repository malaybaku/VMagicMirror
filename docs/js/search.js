$(function () {
  let source = [];
  $.get('ja/api/search-source.json', function (data) {
    source = data;
  });
  $('#search').on('keyup', function () {

    const keyword = this.value.toLowerCase();

    if (keyword.length > 0) {
      $('#search-result').show();
    } else {
      $('#search-result').hide();
    }
    $('.result-item').remove();

    const searchResult = source.reduce( (results, current) => {
        const content = current.content;

        if ( current.title.toLowerCase().indexOf(keyword) >= 0 ||
        content.toLowerCase().indexOf(keyword) >= 0 ){
          results.push( current );
        }

        return results

    }, []);

    if (searchResult.length === 0) {
      $('#search-result').append(
        '<div class="result-item"><div class="description">There is no search result.</div></div>'
      );
    } else {
      searchResult.forEach( item => {
        $('#search-result').append(
          '<a class="result-item" href="' +
            item.url +
            '"><div class="title">' +
            item.title +
            '</div><div class="description">' +
            item.description +
           '</div></a>'
        )
      });
    }

  })
});

$(function () {

  /**
   * fetch source json
   */
  let host = location.host

  // Production home is https://malaybaku.github.io/VMagicMirror/
  if( host.includes( 'github.io' ) ){
    home = `https://${host}/VMagicMirror`;
  } else if( host.includes( 'localhost' ) ) {
    home = `http://${host}`
  }
  const lang = $('html').attr('lang');

  let source = [];
  $.get( `${home}/${lang}/api/search-source.json`, function (data) {
    source = data;
  });

  /**
   * Show search result
   */
  $('#search').on('keyup', function () {

    const keyword = this.value.toLowerCase().trim();

    if (keyword.length > 0) {
      $('#search-result').show();
    } else {
      $('#search-result').hide();
    }
    $('.result-item').remove();

    const searchResult = source.reduce( (results, current) => {
        const content = current.content;

        if ( current.title.toLowerCase().indexOf(keyword) >= 0 ){
          current.content = current.content.substring( 0, 100 );
          results.push( current );
          return results;
        }

        const found = content.toLowerCase().indexOf(keyword);
        if ( found >= 0 ){
          current.content = current.content.substring( found - 50, found + 50 );
          results.push( current );
          return results;
        }

        return results;

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
            item.content +
           '</div></a>'
        )
      });
    }

  })
});

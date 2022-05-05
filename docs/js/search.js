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

    //Does not cancel Materialize js effect, using content editable div
    const keyword = this.innerText.toLowerCase().trim();

    if (keyword.length > 0) {
      $('#search-result-container').removeClass('hidden');
    } else {
      $('#search-result-container').addClass('hidden');
    }

    $('#search-result .result-item').remove();

    const searchResult = source.reduce( (results, current) => {
        const content = current.content;

        if ( current.title.toLowerCase().indexOf(keyword) >= 0 ){
          current.content = current.content.substring( 0, 50 );
          results.push( current );
          return results;
        }

        const found = content.toLowerCase().indexOf(keyword);
        if ( found >= 0 ){
          current.content = current.content.substring( found - 10, found + 30 );
          results.push( current );
          return results;
        }

        return results;

    }, []);

    if (searchResult.length === 0) {
      $('#search-result').append(
        '<div class="collection-header result-item">There is no search result.</div>'
      );
    } else {
      $('#search-result').append(
        '<div class="collection-header result-item">Found pages</div>'
      );
      searchResult.forEach( item => {
        $('#search-result').append(
          '<a class="collection-item result-item" href="' + item.url + '">' +
            item.title + '<br><span>' +
            item.content +
           '</span></a>'
        )
      });
    }

  })
});

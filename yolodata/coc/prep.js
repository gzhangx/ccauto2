const fs = require('fs');

//get Files from labels, match against images, delete extra images, create data.txt file
function createFiles() {
    const lables = fs.readdirSync('labels');
    const images = fs.readdirSync('images');
    const imageNameToLabel = lables.reduce((acc, lbl) => {        
        const imgName = lbl.replace(/.txt$/, '.png')
        acc[imgName] = {
            lblName: lbl,
            matched: false,
        }
        return acc;
    }, {
    });
    
    const goodImgNames = images.map(img => {
        const match = imageNameToLabel[img];
        if (match) {
            match.matched = true;
            return img;
        }         
        console.log(`deleting ${img}`);
        fs.unlinkSync(`images/${img}`);
        return null;
    }).filter(x => x);
    const notMatchedLabels = Object.values(imageNameToLabel).filter(f => !f.matched).map(f => f.lblName);
    if (notMatchedLabels.length) console.log(notMatchedLabels, "not matched labels");
    console.log(goodImgNames);
    fs.writeFileSync('train.txt', goodImgNames.map(l => `${__dirname}/images/${l}`).join('\n'))
    
    const names = fs.readFileSync('names.txt').toString().split('\n').filter(x=>x.trim())
    fs.writeFileSync('data.txt', `classes=${names.length}
train = ${__dirname}/train.txt
valid = ${__dirname}/train.txt
names = ${__dirname}/names.txt
backup = ${__dirname}/backup/
`);
    console.log(__dirname);
}

//https://github.com/pjreddie/darknet/issues/2263
createFiles();